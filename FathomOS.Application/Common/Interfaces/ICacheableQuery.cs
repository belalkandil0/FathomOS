namespace FathomOS.Application.Common.Interfaces;

/// <summary>
/// Interface for queries that support caching.
/// Implement this interface on queries that should be cached for performance.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Gets the cache key for this query.
    /// Should be unique based on the query parameters.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time in seconds.
    /// Returns null to use the default expiration.
    /// </summary>
    int? CacheExpirationInSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether to bypass the cache.
    /// Set to true to force a fresh query execution.
    /// </summary>
    bool BypassCache { get; }
}

/// <summary>
/// Interface for queries that support cache invalidation.
/// </summary>
public interface ICacheInvalidatingCommand
{
    /// <summary>
    /// Gets the cache keys to invalidate when this command succeeds.
    /// </summary>
    IEnumerable<string> CacheKeysToInvalidate { get; }
}
