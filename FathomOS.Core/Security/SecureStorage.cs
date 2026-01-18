// FathomOS.Core/Security/SecureStorage.cs
// Secure settings storage using Windows DPAPI with user-scoped protection

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace FathomOS.Core.Security;

/// <summary>
/// Interface for secure key-value storage using Windows DPAPI encryption.
/// </summary>
public interface ISecureStorage
{
    /// <summary>
    /// Retrieves an encrypted string value.
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Stores a string value with DPAPI encryption.
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Removes a stored value.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Retrieves and deserializes a JSON object.
    /// </summary>
    Task<T?> GetJsonAsync<T>(string key) where T : class;

    /// <summary>
    /// Serializes and stores a JSON object with encryption.
    /// </summary>
    Task SetJsonAsync<T>(string key, T value) where T : class;

    /// <summary>
    /// Checks if a key exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets all stored keys.
    /// </summary>
    Task<IEnumerable<string>> GetAllKeysAsync();

    /// <summary>
    /// Clears all stored values.
    /// </summary>
    Task ClearAllAsync();
}

/// <summary>
/// Provides secure storage for sensitive settings using Windows DPAPI encryption.
///
/// Security features:
/// - User-scoped DPAPI protection (CurrentUser scope)
/// - Additional entropy for stronger encryption
/// - File-based storage with encrypted content
/// - Thread-safe operations
/// - Automatic key derivation per storage item
/// </summary>
public sealed class SecureStorage : ISecureStorage, IDisposable
{
    // Constants
    private const string StorageDirectoryName = "SecureStorage";
    private const string FileExtension = ".enc";
    private const int EntropySize = 32;

    // Product-specific entropy prefix
    private static readonly byte[] EntropyPrefix =
        Encoding.UTF8.GetBytes("FathomOS.SecureStorage.v2");

    // Storage
    private readonly string _storagePath;
    private readonly string _productName;
    private readonly DataProtectionScope _scope;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    // Entropy cache (derived per key for isolation)
    private readonly Dictionary<string, byte[]> _entropyCache = new();

    /// <summary>
    /// Creates a new SecureStorage instance.
    /// </summary>
    /// <param name="productName">Product name for storage isolation.</param>
    /// <param name="scope">DPAPI scope (default: CurrentUser for portability).</param>
    public SecureStorage(string productName = "FathomOS", DataProtectionScope scope = DataProtectionScope.CurrentUser)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));

        _productName = productName;
        _scope = scope;

        // Initialize storage directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storagePath = Path.Combine(appData, _productName, StorageDirectoryName);

        EnsureStorageDirectoryExists();
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key)
    {
        ValidateKey(key);
        ThrowIfDisposed();

        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(key);

            if (!File.Exists(filePath))
                return null;

            var encryptedBytes = await File.ReadAllBytesAsync(filePath);
            var entropy = GetEntropyForKey(key);

            try
            {
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, entropy, _scope);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                // Data corrupted or encrypted with different credentials
                // Remove the corrupted file
                File.Delete(filePath);
                return null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, string value)
    {
        ValidateKey(key);
        ThrowIfDisposed();

        if (value == null)
        {
            await RemoveAsync(key);
            return;
        }

        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(key);
            var entropy = GetEntropyForKey(key);
            var plainBytes = Encoding.UTF8.GetBytes(value);

            try
            {
                var encryptedBytes = ProtectedData.Protect(plainBytes, entropy, _scope);

                // Write atomically using temp file
                var tempPath = filePath + ".tmp";
                await File.WriteAllBytesAsync(tempPath, encryptedBytes);

                // Atomic move
                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.Move(tempPath, filePath);
            }
            finally
            {
                // Clear sensitive data from memory
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        ValidateKey(key);
        ThrowIfDisposed();

        await _lock.WaitAsync();
        try
        {
            var filePath = GetFilePath(key);

            if (File.Exists(filePath))
            {
                // Secure delete - overwrite with random data before deletion
                var fileInfo = new FileInfo(filePath);
                var size = (int)Math.Min(fileInfo.Length, 4096);
                var randomData = new byte[size];
                RandomNumberGenerator.Fill(randomData);

                try
                {
                    await File.WriteAllBytesAsync(filePath, randomData);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(randomData);
                }

                File.Delete(filePath);
            }

            // Clear cached entropy
            lock (_entropyCache)
            {
                if (_entropyCache.TryGetValue(key, out var entropy))
                {
                    CryptographicOperations.ZeroMemory(entropy);
                    _entropyCache.Remove(key);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetJsonAsync<T>(string key) where T : class
    {
        var json = await GetAsync(key);

        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, GetJsonOptions());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SetJsonAsync<T>(string key, T value) where T : class
    {
        if (value == null)
        {
            await RemoveAsync(key);
            return;
        }

        var json = JsonSerializer.Serialize(value, GetJsonOptions());
        await SetAsync(key, json);
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key)
    {
        ValidateKey(key);
        ThrowIfDisposed();

        var filePath = GetFilePath(key);
        return Task.FromResult(File.Exists(filePath));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetAllKeysAsync()
    {
        ThrowIfDisposed();

        if (!Directory.Exists(_storagePath))
            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());

        var keys = Directory.GetFiles(_storagePath, $"*{FileExtension}")
            .Select(f => DecodeKeyFromFileName(Path.GetFileNameWithoutExtension(f)))
            .Where(k => k != null)
            .Cast<string>();

        return Task.FromResult(keys);
    }

    /// <inheritdoc/>
    public async Task ClearAllAsync()
    {
        ThrowIfDisposed();

        await _lock.WaitAsync();
        try
        {
            if (Directory.Exists(_storagePath))
            {
                // Secure delete all files
                foreach (var file in Directory.GetFiles(_storagePath, $"*{FileExtension}"))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var size = (int)Math.Min(fileInfo.Length, 4096);
                        var randomData = new byte[size];
                        RandomNumberGenerator.Fill(randomData);

                        try
                        {
                            await File.WriteAllBytesAsync(file, randomData);
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(randomData);
                        }

                        File.Delete(file);
                    }
                    catch
                    {
                        // Best effort deletion
                    }
                }
            }

            // Clear all cached entropy
            lock (_entropyCache)
            {
                foreach (var entropy in _entropyCache.Values)
                {
                    CryptographicOperations.ZeroMemory(entropy);
                }
                _entropyCache.Clear();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Disposes the secure storage, clearing sensitive data from memory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // Clear all cached entropy
        lock (_entropyCache)
        {
            foreach (var entropy in _entropyCache.Values)
            {
                CryptographicOperations.ZeroMemory(entropy);
            }
            _entropyCache.Clear();
        }

        _lock.Dispose();
        _disposed = true;
    }

    #region Private Methods

    private void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    private string GetFilePath(string key)
    {
        var encodedKey = EncodeKeyToFileName(key);
        return Path.Combine(_storagePath, encodedKey + FileExtension);
    }

    /// <summary>
    /// Derives unique entropy for each key to provide isolation.
    /// </summary>
    private byte[] GetEntropyForKey(string key)
    {
        lock (_entropyCache)
        {
            if (_entropyCache.TryGetValue(key, out var cached))
                return cached;

            // Derive entropy from key using HKDF-like approach
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var combined = new byte[EntropyPrefix.Length + keyBytes.Length];

            Buffer.BlockCopy(EntropyPrefix, 0, combined, 0, EntropyPrefix.Length);
            Buffer.BlockCopy(keyBytes, 0, combined, EntropyPrefix.Length, keyBytes.Length);

            var entropy = SHA256.HashData(combined);

            // Clear combined array
            CryptographicOperations.ZeroMemory(combined);

            _entropyCache[key] = entropy;
            return entropy;
        }
    }

    /// <summary>
    /// Encodes a key to a safe file name using Base64URL encoding.
    /// </summary>
    private static string EncodeKeyToFileName(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);

        // Use first 16 bytes of hash for shorter file names
        return Convert.ToBase64String(hash, 0, 16)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Decodes a file name back to a key (returns null if not possible).
    /// Note: Due to hashing, we can't reverse this - this is intentional for security.
    /// </summary>
    private static string? DecodeKeyFromFileName(string fileName)
    {
        // We can't decode the key from the hash, so return the file name itself
        // This is a limitation but improves security by not exposing key names
        return fileName;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (key.Length > 256)
            throw new ArgumentException("Key length cannot exceed 256 characters", nameof(key));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SecureStorage));
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    #endregion
}

/// <summary>
/// Extension methods for ISecureStorage to simplify common operations.
/// </summary>
public static class SecureStorageExtensions
{
    /// <summary>
    /// Gets a value or returns the default if not found.
    /// </summary>
    public static async Task<string> GetOrDefaultAsync(this ISecureStorage storage, string key, string defaultValue)
    {
        return await storage.GetAsync(key) ?? defaultValue;
    }

    /// <summary>
    /// Gets a value or creates it using the factory if not found.
    /// </summary>
    public static async Task<string> GetOrCreateAsync(this ISecureStorage storage, string key, Func<string> factory)
    {
        var value = await storage.GetAsync(key);

        if (value == null)
        {
            value = factory();
            await storage.SetAsync(key, value);
        }

        return value;
    }

    /// <summary>
    /// Stores a secret and returns its key for later retrieval.
    /// </summary>
    public static async Task<string> StoreSecretAsync(this ISecureStorage storage, string secret, string? keyPrefix = null)
    {
        var key = $"{keyPrefix ?? "secret"}_{Guid.NewGuid():N}";
        await storage.SetAsync(key, secret);
        return key;
    }
}
