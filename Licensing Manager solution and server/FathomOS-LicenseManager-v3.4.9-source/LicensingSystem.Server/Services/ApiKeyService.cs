// LicensingSystem.Server/Services/ApiKeyService.cs
// API key management service for simplified server authentication
// API keys replace username/password admin auth for the tracking-only server

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.Security.Cryptography;
using System.Text;

namespace LicensingSystem.Server.Services;

public interface IApiKeyService
{
    /// <summary>
    /// Validate an API key against stored hash
    /// </summary>
    Task<bool> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// Generate a new API key (returns the plain key - only shown once)
    /// </summary>
    Task<string> GenerateApiKeyAsync();

    /// <summary>
    /// Get or create the API key (for first run)
    /// Returns (apiKey, isFirstRun) - apiKey is the plain key if first run, null otherwise
    /// </summary>
    Task<(string? ApiKey, bool IsFirstRun)> GetOrCreateApiKeyAsync();

    /// <summary>
    /// Check if an API key exists
    /// </summary>
    Task<bool> HasApiKeyAsync();

    /// <summary>
    /// Get the API key hint (last 4 chars for identification)
    /// </summary>
    Task<string?> GetApiKeyHintAsync();

    /// <summary>
    /// Regenerate the API key (invalidates old key)
    /// </summary>
    Task<string> RegenerateApiKeyAsync();
}

public class ApiKeyService : IApiKeyService
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<ApiKeyService> _logger;
    private readonly IConfiguration _config;

    // Cache for performance (validated keys)
    private static readonly Dictionary<string, DateTime> _validatedKeyCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly object _cacheLock = new();

    public ApiKeyService(
        LicenseDbContext db,
        ILogger<ApiKeyService> logger,
        IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _config = config;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        // Check cache first
        lock (_cacheLock)
        {
            if (_validatedKeyCache.TryGetValue(apiKey, out var cachedTime))
            {
                if (DateTime.UtcNow - cachedTime < CacheDuration)
                {
                    return true;
                }
                _validatedKeyCache.Remove(apiKey);
            }
        }

        // Check environment variable first
        var envApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(apiKey),
                Encoding.UTF8.GetBytes(envApiKey)))
            {
                CacheKey(apiKey);
                return true;
            }
        }

        // Check database
        var keyHash = HashApiKey(apiKey);
        var record = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (record != null)
        {
            // Update last used time
            record.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            CacheKey(apiKey);
            return true;
        }

        return false;
    }

    public async Task<string> GenerateApiKeyAsync()
    {
        // Generate a secure 32-byte random key
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);

        // Format as URL-safe base64
        var apiKey = Convert.ToBase64String(keyBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Store hash in database
        var keyHash = HashApiKey(apiKey);
        var keyHint = apiKey[^4..]; // Last 4 chars

        // Deactivate any existing keys
        var existingKeys = await _db.ApiKeys.Where(k => k.IsActive).ToListAsync();
        foreach (var key in existingKeys)
        {
            key.IsActive = false;
        }

        // Create new key record
        var record = new ApiKeyRecord
        {
            KeyHash = keyHash,
            KeyHint = keyHint,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.ApiKeys.Add(record);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New API key generated with hint: ...{KeyHint}", keyHint);

        return apiKey;
    }

    public async Task<(string? ApiKey, bool IsFirstRun)> GetOrCreateApiKeyAsync()
    {
        // Check if environment variable is set
        var envApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            _logger.LogInformation("Using API key from ADMIN_API_KEY environment variable");
            return (null, false); // Don't show key - it's already known via env var
        }

        // Check if we already have an active key
        var existingKey = await _db.ApiKeys.FirstOrDefaultAsync(k => k.IsActive);
        if (existingKey != null)
        {
            _logger.LogInformation("API key exists with hint: ...{KeyHint}", existingKey.KeyHint);
            return (null, false);
        }

        // First run - generate new key
        _logger.LogInformation("First run detected - generating new API key");
        var newApiKey = await GenerateApiKeyAsync();
        return (newApiKey, true);
    }

    public async Task<bool> HasApiKeyAsync()
    {
        // Check env var first
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ADMIN_API_KEY")))
            return true;

        return await _db.ApiKeys.AnyAsync(k => k.IsActive);
    }

    public async Task<string?> GetApiKeyHintAsync()
    {
        var envApiKey = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            return envApiKey.Length >= 4 ? "..." + envApiKey[^4..] : "(env)";
        }

        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.IsActive);
        return key?.KeyHint != null ? "..." + key.KeyHint : null;
    }

    public async Task<string> RegenerateApiKeyAsync()
    {
        _logger.LogWarning("API key regeneration requested - old key will be invalidated");

        // Clear cache
        lock (_cacheLock)
        {
            _validatedKeyCache.Clear();
        }

        return await GenerateApiKeyAsync();
    }

    private static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private void CacheKey(string apiKey)
    {
        lock (_cacheLock)
        {
            _validatedKeyCache[apiKey] = DateTime.UtcNow;

            // Cleanup old entries
            var expired = _validatedKeyCache
                .Where(kvp => DateTime.UtcNow - kvp.Value > CacheDuration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _validatedKeyCache.Remove(key);
            }
        }
    }
}
