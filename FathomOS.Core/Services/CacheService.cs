using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using FathomOS.Core.Interfaces;

namespace FathomOS.Core.Services;

/// <summary>
/// Thread-safe caching service with support for both memory and disk caching,
/// LRU eviction, automatic expiration, and compression.
/// </summary>
public class CacheService : ICacheService, IDisposable
{
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<string, CacheEntryMetadata> _memoryCache = new();
    private readonly LinkedList<string> _lruList = new();
    private readonly object _lruLock = new();
    private readonly Timer _cleanupTimer;
    private readonly CacheStatistics _statistics = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new cache service with default options.
    /// </summary>
    public CacheService() : this(new CacheOptions()) { }

    /// <summary>
    /// Creates a new cache service with specified options.
    /// </summary>
    /// <param name="options">Cache configuration options.</param>
    public CacheService(CacheOptions options)
    {
        _options = options;

        if (_options.EnableDiskCache)
        {
            Directory.CreateDirectory(_options.DiskCachePath);
        }

        _cleanupTimer = new Timer(CleanupExpired, null, _options.CleanupInterval, _options.CleanupInterval);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        return await Task.FromResult(Get<T>(key));
    }

    /// <inheritdoc />
    public T? Get<T>(string key)
    {
        if (_memoryCache.TryGetValue(key, out var metadata))
        {
            if (metadata.IsExpired)
            {
                Remove(key);
                _statistics.Misses++;
                return default;
            }

            metadata.LastAccessedAt = DateTime.UtcNow;
            UpdateLru(key);
            _statistics.Hits++;

            if (metadata.IsOnDisk && metadata.DiskPath != null)
            {
                return LoadFromDisk<T>(metadata.DiskPath);
            }

            return metadata.Value is T typedValue ? typedValue : default;
        }

        _statistics.Misses++;
        return default;
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        await Task.Run(() => Set(key, value, expiration));
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var actualExpiration = expiration ?? _options.DefaultExpiration;
        var expiresAt = actualExpiration.HasValue ? DateTime.UtcNow.Add(actualExpiration.Value) : (DateTime?)null;

        long sizeBytes = EstimateSize(value);
        bool shouldUseDisk = _options.EnableDiskCache && sizeBytes > _options.DiskCacheThreshold;

        string? diskPath = null;
        object? storedValue = value;

        if (shouldUseDisk)
        {
            diskPath = SaveToDisk(key, value);
            storedValue = null; // Don't keep in memory if on disk
        }

        var metadata = new CacheEntryMetadata
        {
            Value = storedValue,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            SizeBytes = sizeBytes,
            IsOnDisk = shouldUseDisk,
            DiskPath = diskPath
        };

        // Evict if necessary
        EnsureCapacity(sizeBytes);

        _memoryCache[key] = metadata;
        AddToLru(key);

        if (!shouldUseDisk)
        {
            _statistics.MemoryBytes += sizeBytes;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        await Task.Run(() => Remove(key));
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        if (_memoryCache.TryRemove(key, out var metadata))
        {
            RemoveFromLru(key);

            if (metadata.IsOnDisk && metadata.DiskPath != null)
            {
                try
                {
                    if (File.Exists(metadata.DiskPath))
                        File.Delete(metadata.DiskPath);
                }
                catch { }
            }
            else
            {
                _statistics.MemoryBytes -= metadata.SizeBytes;
            }
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        await Task.Run(Clear);
    }

    /// <inheritdoc />
    public void Clear()
    {
        var keys = _memoryCache.Keys.ToList();
        foreach (var key in keys)
        {
            Remove(key);
        }

        lock (_lruLock)
        {
            _lruList.Clear();
        }

        _statistics.MemoryBytes = 0;
        _statistics.MemoryItemCount = 0;
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var existing = Get<T>(key);
        if (existing != null)
            return existing;

        var value = await factory();
        Set(key, value, expiration);
        return value;
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        var existing = Get<T>(key);
        if (existing != null)
            return existing;

        var value = factory();
        Set(key, value, expiration);
        return value;
    }

    /// <inheritdoc />
    public bool Contains(string key)
    {
        if (_memoryCache.TryGetValue(key, out var metadata))
        {
            if (metadata.IsExpired)
            {
                Remove(key);
                return false;
            }
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public CacheStatistics GetStatistics()
    {
        _statistics.MemoryItemCount = _memoryCache.Count(kv => !kv.Value.IsOnDisk);
        _statistics.DiskItemCount = _memoryCache.Count(kv => kv.Value.IsOnDisk);

        if (_options.EnableDiskCache && Directory.Exists(_options.DiskCachePath))
        {
            try
            {
                var dirInfo = new DirectoryInfo(_options.DiskCachePath);
                _statistics.DiskBytes = dirInfo.GetFiles().Sum(f => f.Length);
            }
            catch
            {
                _statistics.DiskBytes = 0;
            }
        }

        return new CacheStatistics
        {
            Hits = _statistics.Hits,
            Misses = _statistics.Misses,
            MemoryItemCount = _statistics.MemoryItemCount,
            MemoryBytes = _statistics.MemoryBytes,
            DiskItemCount = _statistics.DiskItemCount,
            DiskBytes = _statistics.DiskBytes,
            Evictions = _statistics.Evictions,
            Expirations = _statistics.Expirations,
            LastCleanup = _statistics.LastCleanup,
            CreatedAt = _statistics.CreatedAt
        };
    }

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
    {
        value = Get<T>(key);
        return value != null;
    }

    #region Private Methods

    private void EnsureCapacity(long requiredBytes)
    {
        // Check item count
        while (_memoryCache.Count >= _options.MaxMemoryItems)
        {
            EvictLru();
        }

        // Check memory size
        while (_statistics.MemoryBytes + requiredBytes > _options.MaxMemoryBytes)
        {
            if (!EvictLru())
                break;
        }
    }

    private bool EvictLru()
    {
        string? keyToEvict = null;

        lock (_lruLock)
        {
            if (_lruList.Count == 0)
                return false;

            keyToEvict = _lruList.Last?.Value;
            if (keyToEvict != null)
                _lruList.RemoveLast();
        }

        if (keyToEvict != null)
        {
            if (_memoryCache.TryRemove(keyToEvict, out var metadata))
            {
                if (metadata.IsOnDisk && metadata.DiskPath != null)
                {
                    try { File.Delete(metadata.DiskPath); } catch { }
                }
                else
                {
                    _statistics.MemoryBytes -= metadata.SizeBytes;
                }
                _statistics.Evictions++;
                return true;
            }
        }

        return false;
    }

    private void UpdateLru(string key)
    {
        lock (_lruLock)
        {
            var node = _lruList.Find(key);
            if (node != null)
            {
                _lruList.Remove(node);
                _lruList.AddFirst(key);
            }
        }
    }

    private void AddToLru(string key)
    {
        lock (_lruLock)
        {
            var existing = _lruList.Find(key);
            if (existing != null)
                _lruList.Remove(existing);
            _lruList.AddFirst(key);
        }
    }

    private void RemoveFromLru(string key)
    {
        lock (_lruLock)
        {
            var node = _lruList.Find(key);
            if (node != null)
                _lruList.Remove(node);
        }
    }

    private void CleanupExpired(object? state)
    {
        var expiredKeys = _memoryCache
            .Where(kv => kv.Value.IsExpired)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            Remove(key);
            _statistics.Expirations++;
        }

        _statistics.LastCleanup = DateTime.UtcNow;
    }

    private string SaveToDisk<T>(string key, T value)
    {
        var safeKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
            .Replace("/", "_")
            .Replace("+", "-");
        var filePath = Path.Combine(_options.DiskCachePath, $"{safeKey}.cache");

        var json = JsonSerializer.Serialize(value);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        if (_options.CompressDiskCache)
        {
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionLevel.Optimal))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            File.WriteAllBytes(filePath, ms.ToArray());
        }
        else
        {
            File.WriteAllBytes(filePath, bytes);
        }

        return filePath;
    }

    private T? LoadFromDisk<T>(string filePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(filePath);

            if (_options.CompressDiskCache)
            {
                using var compressedMs = new MemoryStream(bytes);
                using var gzip = new GZipStream(compressedMs, CompressionMode.Decompress);
                using var decompressedMs = new MemoryStream();
                gzip.CopyTo(decompressedMs);
                bytes = decompressedMs.ToArray();
            }

            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    private static long EstimateSize(object? value)
    {
        if (value == null) return 0;

        return value switch
        {
            string s => s.Length * 2 + 26,
            byte[] b => b.Length + 24,
            Array a => a.Length * 8 + 32,
            _ => 256 // Default estimate for objects
        };
    }

    #endregion

    /// <summary>
    /// Disposes the cache service and cleanup timer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cleanupTimer.Dispose();
        Clear();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal metadata for cache entries.
/// </summary>
internal class CacheEntryMetadata
{
    public object? Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long SizeBytes { get; set; }
    public bool IsOnDisk { get; set; }
    public string? DiskPath { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
