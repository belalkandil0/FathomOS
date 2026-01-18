namespace FathomOS.Core.Interfaces;

/// <summary>
/// Unified caching service interface supporting both memory and disk caching
/// with automatic expiration and LRU eviction policies.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <returns>The cached value or default if not found.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Gets a value from the cache synchronously.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <returns>The cached value or default if not found.</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiration">Optional expiration time. Null = no expiration.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Sets a value in the cache synchronously.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiration">Optional expiration time.</param>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes a value from the cache synchronously.
    /// </summary>
    /// <param name="key">Cache key.</param>
    void Remove(string key);

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Clears all cached values synchronously.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a value from cache, or creates and caches it if not found.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory function to create value if not cached.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Gets a value from cache, or creates and caches it if not found (synchronous).
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Factory function to create value if not cached.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <returns>The cached or newly created value.</returns>
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <returns>True if the key exists and has not expired.</returns>
    bool Contains(string key);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Current cache statistics.</returns>
    CacheStatistics GetStatistics();

    /// <summary>
    /// Tries to get a value from the cache.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if the value was found.</returns>
    bool TryGet<T>(string key, out T? value);
}

/// <summary>
/// Configuration options for the cache service.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Maximum number of items in memory cache before LRU eviction.
    /// </summary>
    public int MaxMemoryItems { get; set; } = 1000;

    /// <summary>
    /// Maximum size of memory cache in bytes (approximate).
    /// </summary>
    public long MaxMemoryBytes { get; set; } = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Default expiration time for cached items. Null = no default expiration.
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Enable disk caching for large objects.
    /// </summary>
    public bool EnableDiskCache { get; set; } = true;

    /// <summary>
    /// Directory for disk cache storage.
    /// </summary>
    public string DiskCachePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FathomOS", "Cache");

    /// <summary>
    /// Maximum disk cache size in bytes.
    /// </summary>
    public long MaxDiskBytes { get; set; } = 1024 * 1024 * 1024; // 1 GB

    /// <summary>
    /// Threshold size in bytes above which items are stored on disk.
    /// </summary>
    public int DiskCacheThreshold { get; set; } = 100 * 1024; // 100 KB

    /// <summary>
    /// Enable compression for disk-cached items.
    /// </summary>
    public bool CompressDiskCache { get; set; } = true;

    /// <summary>
    /// Interval for cleanup of expired items.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Cache statistics for monitoring.
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Number of cache hits.
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Number of cache misses.
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Current number of items in memory cache.
    /// </summary>
    public int MemoryItemCount { get; set; }

    /// <summary>
    /// Approximate memory usage in bytes.
    /// </summary>
    public long MemoryBytes { get; set; }

    /// <summary>
    /// Current number of items in disk cache.
    /// </summary>
    public int DiskItemCount { get; set; }

    /// <summary>
    /// Disk cache usage in bytes.
    /// </summary>
    public long DiskBytes { get; set; }

    /// <summary>
    /// Number of items evicted due to capacity limits.
    /// </summary>
    public long Evictions { get; set; }

    /// <summary>
    /// Number of expired items removed.
    /// </summary>
    public long Expirations { get; set; }

    /// <summary>
    /// Hit rate as a percentage.
    /// </summary>
    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;

    /// <summary>
    /// Last cleanup time.
    /// </summary>
    public DateTime? LastCleanup { get; set; }

    /// <summary>
    /// Cache creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache entry metadata.
/// </summary>
public class CacheEntry<T>
{
    /// <summary>
    /// The cached value.
    /// </summary>
    public T Value { get; set; } = default!;

    /// <summary>
    /// When the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the entry was last accessed.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Absolute expiration time. Null = no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Approximate size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Whether this entry is stored on disk.
    /// </summary>
    public bool IsOnDisk { get; set; }

    /// <summary>
    /// Disk file path if stored on disk.
    /// </summary>
    public string? DiskPath { get; set; }

    /// <summary>
    /// Check if the entry has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
