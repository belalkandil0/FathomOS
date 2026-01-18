// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/RateLimiter.cs
// Purpose: Rate limiting for NaviPac connections (MISSING-007)
// Version: 1.0 - Initial implementation with token bucket algorithm
// ============================================================================
//
// SECURITY NOTES:
// - Implements token bucket algorithm for smooth rate limiting
// - Tracks connections per IP address
// - Logs rate limit violations for security monitoring
// - Configurable thresholds for different deployment scenarios
//
// ============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Enable rate limiting.
    /// Default: true for security.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of connections allowed per IP per time window.
    /// Default: 10 connections per minute.
    /// </summary>
    public int MaxConnectionsPerIp { get; set; } = 10;

    /// <summary>
    /// Time window for connection rate limiting in seconds.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int TimeWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of tokens in the bucket (burst capacity).
    /// Default: 15 to allow some burst traffic.
    /// </summary>
    public int BucketCapacity { get; set; } = 15;

    /// <summary>
    /// Token refill rate per second.
    /// Default: 0.167 (10 tokens per minute).
    /// </summary>
    public double TokenRefillRatePerSecond { get; set; } = 10.0 / 60.0;

    /// <summary>
    /// Enable logging of rate limit violations.
    /// </summary>
    public bool LogViolations { get; set; } = true;

    /// <summary>
    /// Maximum number of IPs to track (prevents memory exhaustion attacks).
    /// </summary>
    public int MaxTrackedIps { get; set; } = 1000;

    /// <summary>
    /// Time in seconds before an IP entry expires and is removed.
    /// </summary>
    public int IpEntryExpirationSeconds { get; set; } = 300;

    /// <summary>
    /// Whitelist of IP addresses that bypass rate limiting.
    /// </summary>
    public HashSet<string> WhitelistedIps { get; set; } = new()
    {
        "127.0.0.1",
        "::1"
    };

    /// <summary>
    /// Blacklist of IP addresses that are always blocked.
    /// </summary>
    public HashSet<string> BlacklistedIps { get; set; } = new();

    /// <summary>
    /// Time in seconds to temporarily block an IP after repeated violations.
    /// </summary>
    public int BlockDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Number of violations before temporarily blocking an IP.
    /// </summary>
    public int ViolationsBeforeBlock { get; set; } = 5;
}

/// <summary>
/// Token bucket state for an individual IP address.
/// </summary>
internal class TokenBucket
{
    public double Tokens { get; set; }
    public DateTime LastRefill { get; set; }
    public DateTime LastAccess { get; set; }
    public int ViolationCount { get; set; }
    public DateTime? BlockedUntil { get; set; }

    public TokenBucket(double initialTokens)
    {
        Tokens = initialTokens;
        LastRefill = DateTime.UtcNow;
        LastAccess = DateTime.UtcNow;
        ViolationCount = 0;
        BlockedUntil = null;
    }
}

/// <summary>
/// Result of a rate limit check.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Reason for denial (if not allowed).
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Remaining tokens in the bucket.
    /// </summary>
    public double RemainingTokens { get; set; }

    /// <summary>
    /// Time until tokens are refilled (if denied).
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Whether the IP is temporarily blocked.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Time until the block expires.
    /// </summary>
    public DateTime? BlockExpiresAt { get; set; }

    public static RateLimitResult Allowed(double remainingTokens) => new()
    {
        IsAllowed = true,
        RemainingTokens = remainingTokens
    };

    public static RateLimitResult Denied(string reason, double remainingTokens, TimeSpan? retryAfter = null) => new()
    {
        IsAllowed = false,
        DenialReason = reason,
        RemainingTokens = remainingTokens,
        RetryAfter = retryAfter
    };

    public static RateLimitResult Blocked(DateTime blockedUntil) => new()
    {
        IsAllowed = false,
        DenialReason = "IP address is temporarily blocked due to repeated violations",
        IsBlocked = true,
        BlockExpiresAt = blockedUntil
    };

    public static RateLimitResult Blacklisted() => new()
    {
        IsAllowed = false,
        DenialReason = "IP address is blacklisted",
        IsBlocked = true
    };
}

/// <summary>
/// Event args for rate limit violations.
/// </summary>
public class RateLimitViolationEventArgs : EventArgs
{
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int ViolationCount { get; set; }
    public DateTime Timestamp { get; set; }
    public bool WasBlocked { get; set; }
}

/// <summary>
/// Implements rate limiting using the token bucket algorithm.
/// Tracks connections per IP address and enforces configurable limits.
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly RateLimitConfiguration _config;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly System.Threading.Timer _cleanupTimer;
    private readonly object _cleanupLock = new();
    private bool _disposed;

    // Logging
    private readonly string _logPath;
    private StreamWriter? _logWriter;

    /// <summary>
    /// Event raised when a rate limit violation occurs.
    /// </summary>
    public event EventHandler<RateLimitViolationEventArgs>? ViolationOccurred;

    /// <summary>
    /// Gets the current number of tracked IP addresses.
    /// </summary>
    public int TrackedIpCount => _buckets.Count;

    /// <summary>
    /// Initializes a new rate limiter with the specified configuration.
    /// </summary>
    public RateLimiter(RateLimitConfiguration? config = null)
    {
        _config = config ?? new RateLimitConfiguration();
        _buckets = new ConcurrentDictionary<string, TokenBucket>();

        // Setup cleanup timer (runs every 60 seconds)
        _cleanupTimer = new System.Threading.Timer(CleanupExpiredEntries, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

        // Setup log file
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "SurveyLogbook", "Logs");
        Directory.CreateDirectory(logFolder);
        _logPath = Path.Combine(logFolder, $"RateLimit_{DateTime.Now:yyyyMMdd}.log");

        if (_config.LogViolations)
        {
            try
            {
                _logWriter = new StreamWriter(_logPath, true) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RateLimiter: Failed to open log file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Checks if a connection from the specified IP address should be allowed.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>Rate limit result indicating if the connection is allowed.</returns>
    public RateLimitResult CheckConnection(string ipAddress)
    {
        if (!_config.Enabled)
            return RateLimitResult.Allowed(double.MaxValue);

        if (string.IsNullOrWhiteSpace(ipAddress))
            return RateLimitResult.Allowed(double.MaxValue);

        // Normalize IP address
        ipAddress = NormalizeIpAddress(ipAddress);

        // Check blacklist
        if (_config.BlacklistedIps.Contains(ipAddress))
        {
            LogViolation(ipAddress, "Blacklisted IP attempted connection", 0, false);
            return RateLimitResult.Blacklisted();
        }

        // Check whitelist
        if (_config.WhitelistedIps.Contains(ipAddress))
            return RateLimitResult.Allowed(double.MaxValue);

        // Get or create bucket for this IP
        var bucket = _buckets.GetOrAdd(ipAddress, _ => new TokenBucket(_config.BucketCapacity));

        lock (bucket)
        {
            var now = DateTime.UtcNow;
            bucket.LastAccess = now;

            // Check if IP is blocked
            if (bucket.BlockedUntil.HasValue && bucket.BlockedUntil.Value > now)
            {
                return RateLimitResult.Blocked(bucket.BlockedUntil.Value);
            }

            // Clear block if expired
            if (bucket.BlockedUntil.HasValue && bucket.BlockedUntil.Value <= now)
            {
                bucket.BlockedUntil = null;
                bucket.ViolationCount = 0;
            }

            // Refill tokens based on time elapsed
            var timeSinceRefill = now - bucket.LastRefill;
            var tokensToAdd = timeSinceRefill.TotalSeconds * _config.TokenRefillRatePerSecond;
            bucket.Tokens = Math.Min(bucket.Tokens + tokensToAdd, _config.BucketCapacity);
            bucket.LastRefill = now;

            // Check if we have enough tokens
            if (bucket.Tokens >= 1.0)
            {
                bucket.Tokens -= 1.0;
                return RateLimitResult.Allowed(bucket.Tokens);
            }

            // Rate limited - record violation
            bucket.ViolationCount++;

            // Calculate retry time
            var tokensNeeded = 1.0 - bucket.Tokens;
            var secondsToWait = tokensNeeded / _config.TokenRefillRatePerSecond;
            var retryAfter = TimeSpan.FromSeconds(Math.Ceiling(secondsToWait));

            // Check if should block
            var wasBlocked = false;
            if (bucket.ViolationCount >= _config.ViolationsBeforeBlock)
            {
                bucket.BlockedUntil = now.AddSeconds(_config.BlockDurationSeconds);
                wasBlocked = true;
            }

            LogViolation(ipAddress, "Rate limit exceeded", bucket.ViolationCount, wasBlocked);

            if (wasBlocked)
            {
                return RateLimitResult.Blocked(bucket.BlockedUntil!.Value);
            }

            return RateLimitResult.Denied(
                $"Rate limit exceeded. Try again in {retryAfter.TotalSeconds:F0} seconds.",
                bucket.Tokens,
                retryAfter);
        }
    }

    /// <summary>
    /// Checks if a connection from the specified endpoint should be allowed.
    /// </summary>
    /// <param name="endpoint">The IP endpoint to check.</param>
    /// <returns>Rate limit result indicating if the connection is allowed.</returns>
    public RateLimitResult CheckConnection(IPEndPoint? endpoint)
    {
        if (endpoint == null)
            return RateLimitResult.Allowed(double.MaxValue);

        return CheckConnection(endpoint.Address.ToString());
    }

    /// <summary>
    /// Manually blocks an IP address.
    /// </summary>
    /// <param name="ipAddress">IP address to block.</param>
    /// <param name="durationSeconds">Duration of the block in seconds.</param>
    public void BlockIp(string ipAddress, int? durationSeconds = null)
    {
        ipAddress = NormalizeIpAddress(ipAddress);
        var duration = durationSeconds ?? _config.BlockDurationSeconds;

        var bucket = _buckets.GetOrAdd(ipAddress, _ => new TokenBucket(_config.BucketCapacity));
        lock (bucket)
        {
            bucket.BlockedUntil = DateTime.UtcNow.AddSeconds(duration);
        }

        LogViolation(ipAddress, $"Manually blocked for {duration} seconds", 0, true);
    }

    /// <summary>
    /// Unblocks an IP address.
    /// </summary>
    /// <param name="ipAddress">IP address to unblock.</param>
    public void UnblockIp(string ipAddress)
    {
        ipAddress = NormalizeIpAddress(ipAddress);

        if (_buckets.TryGetValue(ipAddress, out var bucket))
        {
            lock (bucket)
            {
                bucket.BlockedUntil = null;
                bucket.ViolationCount = 0;
            }
        }

        Debug.WriteLine($"RateLimiter: Unblocked IP {ipAddress}");
    }

    /// <summary>
    /// Adds an IP address to the whitelist.
    /// </summary>
    public void AddToWhitelist(string ipAddress)
    {
        ipAddress = NormalizeIpAddress(ipAddress);
        _config.WhitelistedIps.Add(ipAddress);
        _config.BlacklistedIps.Remove(ipAddress);
    }

    /// <summary>
    /// Adds an IP address to the blacklist.
    /// </summary>
    public void AddToBlacklist(string ipAddress)
    {
        ipAddress = NormalizeIpAddress(ipAddress);
        _config.BlacklistedIps.Add(ipAddress);
        _config.WhitelistedIps.Remove(ipAddress);
    }

    /// <summary>
    /// Removes an IP address from the whitelist.
    /// </summary>
    public void RemoveFromWhitelist(string ipAddress)
    {
        ipAddress = NormalizeIpAddress(ipAddress);
        _config.WhitelistedIps.Remove(ipAddress);
    }

    /// <summary>
    /// Removes an IP address from the blacklist.
    /// </summary>
    public void RemoveFromBlacklist(string ipAddress)
    {
        ipAddress = NormalizeIpAddress(ipAddress);
        _config.BlacklistedIps.Remove(ipAddress);
    }

    /// <summary>
    /// Gets statistics for a specific IP address.
    /// </summary>
    public RateLimitStatistics? GetStatistics(string ipAddress)
    {
        ipAddress = NormalizeIpAddress(ipAddress);

        if (!_buckets.TryGetValue(ipAddress, out var bucket))
            return null;

        lock (bucket)
        {
            return new RateLimitStatistics
            {
                IpAddress = ipAddress,
                RemainingTokens = bucket.Tokens,
                ViolationCount = bucket.ViolationCount,
                LastAccess = bucket.LastAccess,
                IsBlocked = bucket.BlockedUntil.HasValue && bucket.BlockedUntil.Value > DateTime.UtcNow,
                BlockedUntil = bucket.BlockedUntil
            };
        }
    }

    /// <summary>
    /// Gets statistics for all tracked IP addresses.
    /// </summary>
    public IReadOnlyList<RateLimitStatistics> GetAllStatistics()
    {
        var stats = new List<RateLimitStatistics>();

        foreach (var kvp in _buckets)
        {
            var stat = GetStatistics(kvp.Key);
            if (stat != null)
                stats.Add(stat);
        }

        return stats.OrderByDescending(s => s.ViolationCount).ToList();
    }

    /// <summary>
    /// Resets all rate limiting data.
    /// </summary>
    public void Reset()
    {
        _buckets.Clear();
        Debug.WriteLine("RateLimiter: All data reset");
    }

    private static string NormalizeIpAddress(string ipAddress)
    {
        // Handle IPv4-mapped IPv6 addresses
        if (ipAddress.StartsWith("::ffff:"))
            return ipAddress.Substring(7);

        return ipAddress.Trim();
    }

    private void LogViolation(string ipAddress, string reason, int violationCount, bool wasBlocked)
    {
        var args = new RateLimitViolationEventArgs
        {
            IpAddress = ipAddress,
            Reason = reason,
            ViolationCount = violationCount,
            Timestamp = DateTime.Now,
            WasBlocked = wasBlocked
        };

        ViolationOccurred?.Invoke(this, args);

        if (_config.LogViolations)
        {
            var logEntry = $"[{args.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] " +
                          $"IP={ipAddress} | " +
                          $"Reason={reason} | " +
                          $"ViolationCount={violationCount} | " +
                          $"Blocked={wasBlocked}";

            Debug.WriteLine($"RateLimiter: {logEntry}");

            try
            {
                _logWriter?.WriteLine(logEntry);
            }
            catch { }
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        if (_disposed)
            return;

        lock (_cleanupLock)
        {
            var now = DateTime.UtcNow;
            var expirationThreshold = now.AddSeconds(-_config.IpEntryExpirationSeconds);
            var ipsToRemove = new List<string>();

            foreach (var kvp in _buckets)
            {
                var bucket = kvp.Value;
                lock (bucket)
                {
                    // Remove entries that haven't been accessed recently
                    // and aren't blocked
                    if (bucket.LastAccess < expirationThreshold &&
                        (!bucket.BlockedUntil.HasValue || bucket.BlockedUntil.Value < now))
                    {
                        ipsToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var ip in ipsToRemove)
            {
                _buckets.TryRemove(ip, out _);
            }

            if (ipsToRemove.Count > 0)
            {
                Debug.WriteLine($"RateLimiter: Cleaned up {ipsToRemove.Count} expired entries");
            }

            // If we're over the limit, remove oldest entries
            if (_buckets.Count > _config.MaxTrackedIps)
            {
                var excess = _buckets.Count - _config.MaxTrackedIps;
                var oldestIps = _buckets
                    .OrderBy(kvp => kvp.Value.LastAccess)
                    .Take(excess)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var ip in oldestIps)
                {
                    _buckets.TryRemove(ip, out _);
                }

                Debug.WriteLine($"RateLimiter: Removed {oldestIps.Count} oldest entries (capacity limit)");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cleanupTimer.Dispose();

        try
        {
            _logWriter?.Close();
            _logWriter?.Dispose();
        }
        catch { }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Statistics for a rate-limited IP address.
/// </summary>
public class RateLimitStatistics
{
    public string IpAddress { get; set; } = string.Empty;
    public double RemainingTokens { get; set; }
    public int ViolationCount { get; set; }
    public DateTime LastAccess { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
}
