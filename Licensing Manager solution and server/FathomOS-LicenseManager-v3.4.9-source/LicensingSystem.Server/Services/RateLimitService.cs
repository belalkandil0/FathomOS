// LicensingSystem.Server/Services/RateLimitService.cs
// Rate limiting and IP blocking service

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;

namespace LicensingSystem.Server.Services;

public interface IRateLimitService
{
    Task<bool> CheckRateLimitAsync(string key, string endpoint, int maxRequests, TimeSpan window);
    Task<bool> IsIpBlockedAsync(string ipAddress);
    Task BlockIpAsync(string ipAddress, string reason, TimeSpan? duration = null, string? blockedBy = null);
    Task UnblockIpAsync(string ipAddress);
    Task<List<BlockedIpRecord>> GetBlockedIpsAsync();
    Task CleanupExpiredAsync();
}

public class RateLimitService : IRateLimitService
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<RateLimitService> _logger;
    
    // In-memory cache for performance (with database persistence for longer blocks)
    private static readonly Dictionary<string, (int Count, DateTime WindowStart)> _rateLimitCache = new();
    private static readonly HashSet<string> _blockedIpCache = new();
    private static DateTime _lastCacheRefresh = DateTime.MinValue;
    private static readonly object _cacheLock = new();

    public RateLimitService(LicenseDbContext db, ILogger<RateLimitService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> CheckRateLimitAsync(string key, string endpoint, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var fullKey = $"{key}:{endpoint}";

        lock (_cacheLock)
        {
            if (_rateLimitCache.TryGetValue(fullKey, out var cached))
            {
                // Check if window has expired
                if (now - cached.WindowStart > window)
                {
                    // Reset window
                    _rateLimitCache[fullKey] = (1, now);
                    return true;
                }

                // Increment and check
                if (cached.Count >= maxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for {Key} on {Endpoint}: {Count}/{Max}", 
                        key, endpoint, cached.Count, maxRequests);
                    return false;
                }

                _rateLimitCache[fullKey] = (cached.Count + 1, cached.WindowStart);
                return true;
            }
            else
            {
                // First request
                _rateLimitCache[fullKey] = (1, now);
                return true;
            }
        }
    }

    public async Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        bool needsCacheRefresh = false;
        bool inCache = false;

        // Check in-memory cache first (quick check)
        lock (_cacheLock)
        {
            // Check if cache needs refresh
            if ((DateTime.UtcNow - _lastCacheRefresh).TotalMinutes > 5)
            {
                needsCacheRefresh = true;
            }
            else
            {
                inCache = _blockedIpCache.Contains(ipAddress);
            }
        }

        // Refresh cache outside the lock to avoid deadlock
        if (needsCacheRefresh)
        {
            await RefreshBlockedIpCacheAsync();
            
            // Re-check cache after refresh
            lock (_cacheLock)
            {
                if (_blockedIpCache.Contains(ipAddress))
                    return true;
            }
        }
        else if (inCache)
        {
            return true;
        }

        // Double-check database for permanent blocks
        var block = await _db.BlockedIps
            .FirstOrDefaultAsync(b => b.IpAddress == ipAddress && b.IsActive);

        if (block != null)
        {
            // Check if expired
            if (block.ExpiresAt.HasValue && block.ExpiresAt < DateTime.UtcNow)
            {
                block.IsActive = false;
                await _db.SaveChangesAsync();
                return false;
            }

            return true;
        }

        return false;
    }

    public async Task BlockIpAsync(string ipAddress, string reason, TimeSpan? duration = null, string? blockedBy = null)
    {
        var existing = await _db.BlockedIps.FirstOrDefaultAsync(b => b.IpAddress == ipAddress);
        
        if (existing != null)
        {
            existing.IsActive = true;
            existing.Reason = reason;
            existing.BlockedAt = DateTime.UtcNow;
            existing.ExpiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null;
            existing.BlockedBy = blockedBy;
        }
        else
        {
            _db.BlockedIps.Add(new BlockedIpRecord
            {
                IpAddress = ipAddress,
                Reason = reason,
                ExpiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null,
                BlockedBy = blockedBy
            });
        }

        await _db.SaveChangesAsync();

        lock (_cacheLock)
        {
            _blockedIpCache.Add(ipAddress);
        }

        _logger.LogWarning("Blocked IP {IpAddress}: {Reason} (Duration: {Duration})", 
            ipAddress, reason, duration?.ToString() ?? "permanent");
    }

    public async Task UnblockIpAsync(string ipAddress)
    {
        var block = await _db.BlockedIps.FirstOrDefaultAsync(b => b.IpAddress == ipAddress);
        
        if (block != null)
        {
            block.IsActive = false;
            await _db.SaveChangesAsync();
        }

        lock (_cacheLock)
        {
            _blockedIpCache.Remove(ipAddress);
        }

        _logger.LogInformation("Unblocked IP {IpAddress}", ipAddress);
    }

    public async Task<List<BlockedIpRecord>> GetBlockedIpsAsync()
    {
        return await _db.BlockedIps
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.BlockedAt)
            .ToListAsync();
    }

    public async Task CleanupExpiredAsync()
    {
        var now = DateTime.UtcNow;

        // Cleanup expired blocks
        var expiredBlocks = await _db.BlockedIps
            .Where(b => b.IsActive && b.ExpiresAt.HasValue && b.ExpiresAt < now)
            .ToListAsync();

        foreach (var block in expiredBlocks)
        {
            block.IsActive = false;
        }

        // Cleanup old rate limit records (keep last 24 hours)
        var oldRateLimits = await _db.RateLimits
            .Where(r => r.WindowStart < now.AddDays(-1))
            .ToListAsync();

        _db.RateLimits.RemoveRange(oldRateLimits);

        await _db.SaveChangesAsync();

        // Refresh cache
        await RefreshBlockedIpCacheAsync();

        // Clear old entries from in-memory rate limit cache
        lock (_cacheLock)
        {
            var expiredKeys = _rateLimitCache
                .Where(kvp => (now - kvp.Value.WindowStart).TotalHours > 1)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _rateLimitCache.Remove(key);
            }
        }

        _logger.LogInformation("Cleaned up {ExpiredBlocks} expired blocks and {OldRateLimits} old rate limit records",
            expiredBlocks.Count, oldRateLimits.Count);
    }

    private async Task RefreshBlockedIpCacheAsync()
    {
        var blockedIps = await _db.BlockedIps
            .Where(b => b.IsActive && (!b.ExpiresAt.HasValue || b.ExpiresAt > DateTime.UtcNow))
            .Select(b => b.IpAddress)
            .ToListAsync();

        lock (_cacheLock)
        {
            _blockedIpCache.Clear();
            foreach (var ip in blockedIps)
            {
                _blockedIpCache.Add(ip);
            }
            _lastCacheRefresh = DateTime.UtcNow;
        }
    }
}
