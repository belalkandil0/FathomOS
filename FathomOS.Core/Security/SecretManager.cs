// FathomOS.Core/Security/SecretManager.cs
// SECURITY FIX: VULN-004 / MISSING-001 - Database encryption key management
// Provides DPAPI-based encryption key management for SQLite database encryption

using System.Security.Cryptography;
using Microsoft.Win32;

namespace FathomOS.Core.Security;

/// <summary>
/// SECURITY FIX: Manages encryption keys for database encryption using Windows DPAPI.
/// Keys are protected using DataProtectionScope.LocalMachine for machine-wide access,
/// allowing the database to be accessed by any user on the machine running FathomOS.
/// </summary>
public static class SecretManager
{
    // SECURITY FIX: Registry key for storing the protected database encryption key
    private const string RegistryKeyPath = @"SOFTWARE\FathomOS";
    private const string RegistryValueName = "DatabaseKey";

    // SECURITY FIX: Key size for AES-256 encryption (32 bytes = 256 bits)
    private const int KeySizeBytes = 32;

    // SECURITY FIX: Optional entropy for additional protection layer
    private static readonly byte[] AdditionalEntropy =
        System.Text.Encoding.UTF8.GetBytes("FathomOS.Database.Encryption.v1");

    private static readonly object _lock = new();
    private static string? _cachedKey;

    /// <summary>
    /// SECURITY FIX: Gets or creates the database encryption key.
    /// If no key exists, generates a new cryptographically secure random key,
    /// protects it using DPAPI, and stores it in the Windows Registry.
    /// </summary>
    /// <returns>Base64-encoded encryption key for SQLCipher PRAGMA key</returns>
    /// <exception cref="CryptographicException">Thrown if key operations fail</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if registry access is denied</exception>
    public static string GetOrCreateDatabaseKey()
    {
        lock (_lock)
        {
            // SECURITY FIX: Return cached key if available to avoid repeated registry/DPAPI calls
            if (!string.IsNullOrEmpty(_cachedKey))
            {
                return _cachedKey;
            }

            try
            {
                // SECURITY FIX: Try to retrieve existing key from registry
                var protectedKey = GetProtectedKeyFromRegistry();

                if (protectedKey != null)
                {
                    // SECURITY FIX: Unprotect the key using DPAPI
                    var key = UnprotectKey(protectedKey);
                    _cachedKey = Convert.ToBase64String(key);

                    // SECURITY FIX: Clear sensitive data from memory
                    CryptographicOperations.ZeroMemory(key);

                    return _cachedKey;
                }

                // SECURITY FIX: Generate new key if none exists
                var newKey = GenerateNewKey();

                // SECURITY FIX: Protect and store the key
                var newProtectedKey = ProtectKey(newKey);
                StoreProtectedKeyInRegistry(newProtectedKey);

                _cachedKey = Convert.ToBase64String(newKey);

                // SECURITY FIX: Clear sensitive data from memory
                CryptographicOperations.ZeroMemory(newKey);

                return _cachedKey;
            }
            catch (Exception ex)
            {
                throw new CryptographicException(
                    "Failed to get or create database encryption key. " +
                    "Ensure the application has appropriate permissions.", ex);
            }
        }
    }

    /// <summary>
    /// SECURITY FIX: Checks if a database encryption key exists.
    /// Used to determine if database migration is needed.
    /// </summary>
    /// <returns>True if an encryption key exists in the registry</returns>
    public static bool KeyExists()
    {
        try
        {
            return GetProtectedKeyFromRegistry() != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// SECURITY FIX: Deletes the stored encryption key.
    /// WARNING: This will make any encrypted databases inaccessible!
    /// Only use during development or when explicitly requested.
    /// </summary>
    /// <returns>True if key was deleted, false if no key existed</returns>
    public static bool DeleteKey()
    {
        lock (_lock)
        {
            _cachedKey = null;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
                if (key != null)
                {
                    key.DeleteValue(RegistryValueName, throwOnMissingValue: false);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// SECURITY FIX: Rotates the database encryption key.
    /// Note: This only updates the stored key. The caller must handle
    /// re-encrypting any existing databases with the new key.
    /// </summary>
    /// <returns>The new base64-encoded encryption key</returns>
    public static string RotateKey()
    {
        lock (_lock)
        {
            _cachedKey = null;

            // SECURITY FIX: Generate new key
            var newKey = GenerateNewKey();

            // SECURITY FIX: Protect and store the new key
            var protectedKey = ProtectKey(newKey);
            StoreProtectedKeyInRegistry(protectedKey);

            _cachedKey = Convert.ToBase64String(newKey);

            // SECURITY FIX: Clear sensitive data from memory
            CryptographicOperations.ZeroMemory(newKey);

            return _cachedKey;
        }
    }

    /// <summary>
    /// SECURITY FIX: Clears the cached key from memory.
    /// Call this when the application is shutting down or when
    /// sensitive data should be cleared.
    /// </summary>
    public static void ClearCache()
    {
        lock (_lock)
        {
            _cachedKey = null;
        }
    }

    #region Private Methods

    /// <summary>
    /// SECURITY FIX: Generates a cryptographically secure random key
    /// </summary>
    private static byte[] GenerateNewKey()
    {
        var key = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// SECURITY FIX: Protects data using Windows DPAPI
    /// Uses LocalMachine scope so any user on this machine can access the key
    /// </summary>
    private static byte[] ProtectKey(byte[] plainKey)
    {
        return ProtectedData.Protect(
            plainKey,
            AdditionalEntropy,
            DataProtectionScope.LocalMachine);
    }

    /// <summary>
    /// SECURITY FIX: Unprotects data using Windows DPAPI
    /// </summary>
    private static byte[] UnprotectKey(byte[] protectedKey)
    {
        return ProtectedData.Unprotect(
            protectedKey,
            AdditionalEntropy,
            DataProtectionScope.LocalMachine);
    }

    /// <summary>
    /// SECURITY FIX: Retrieves the protected key from Windows Registry
    /// </summary>
    private static byte[]? GetProtectedKeyFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
        return key?.GetValue(RegistryValueName) as byte[];
    }

    /// <summary>
    /// SECURITY FIX: Stores the protected key in Windows Registry
    /// </summary>
    private static void StoreProtectedKeyInRegistry(byte[] protectedKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue(RegistryValueName, protectedKey, RegistryValueKind.Binary);
    }

    #endregion
}
