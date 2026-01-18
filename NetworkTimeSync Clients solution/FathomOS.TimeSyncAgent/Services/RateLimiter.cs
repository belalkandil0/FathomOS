using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FathomOS.TimeSyncAgent.Models;

namespace FathomOS.TimeSyncAgent.Services;

/// <summary>
/// Rate limiter for the TCP listener service.
/// Provides per-IP connection throttling and brute-force protection with exponential backoff.
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly RateLimitSettings _settings;
    private readonly ILogger<RateLimiter> _logger;
    private readonly HashSet<string> _whitelistedIps;

    // Track requests per IP (IP -> list of request timestamps)
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestsPerIp = new();

    // Track total requests (list of all request timestamps)
    private readonly ConcurrentQueue<DateTime> _totalRequests = new();

    // Track failed authentication attempts per IP
    private readonly ConcurrentDictionary<string, FailedAttemptInfo> _failedAttempts = new();

    // Track blocked IPs with their unblock time
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();

    // Cleanup timer
    private readonly Timer _cleanupTimer;
    private readonly object _cleanupLock = new();
    private bool _disposed;

    public RateLimiter(IOptions<RateLimitSettings> settings, ILogger<RateLimiter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _whitelistedIps = new HashSet<string>(_settings.GetWhitelistedIps(), StringComparer.OrdinalIgnoreCase);

        // Start cleanup timer
        var cleanupInterval = TimeSpan.FromMinutes(_settings.CleanupIntervalMinutes);
        _cleanupTimer = new Timer(CleanupExpiredData, null, cleanupInterval, cleanupInterval);
    }

    /// <summary>
    /// Result of a rate limit check.
    /// </summary>
    public enum RateLimitResult
    {
        /// <summary>Request is allowed.</summary>
        Allowed,

        /// <summary>Blocked due to per-IP rate limit exceeded.</summary>
        IpRateLimitExceeded,

        /// <summary>Blocked due to total rate limit exceeded.</summary>
        TotalRateLimitExceeded,

        /// <summary>Blocked due to exponential backoff from failed attempts.</summary>
        BlockedByBackoff,

        /// <summary>Rate limiting is disabled.</summary>
        Disabled
    }

    /// <summary>
    /// Checks if a request from the given IP is allowed based on rate limits.
    /// </summary>
    /// <param name="ipAddress">The IP address of the client.</param>
    /// <returns>The rate limit result and optional retry-after time in seconds.</returns>
    public (RateLimitResult Result, int? RetryAfterSeconds) CheckRateLimit(string ipAddress)
    {
        if (!_settings.Enabled)
            return (RateLimitResult.Disabled, null);

        var normalizedIp = NormalizeIpAddress(ipAddress);

        // Check whitelist
        if (_whitelistedIps.Contains(normalizedIp))
            return (RateLimitResult.Allowed, null);

        var now = DateTime.UtcNow;

        // Check if IP is blocked due to backoff
        if (_blockedIps.TryGetValue(normalizedIp, out var unblockTime))
        {
            if (now < unblockTime)
            {
                var retryAfter = (int)Math.Ceiling((unblockTime - now).TotalSeconds);
                if (_settings.LogViolations)
                {
                    _logger.LogWarning(
                        "Rate limit: IP {IpAddress} is blocked due to backoff. Retry after {RetryAfter} seconds",
                        normalizedIp, retryAfter);
                }
                return (RateLimitResult.BlockedByBackoff, retryAfter);
            }

            // Backoff period expired, remove from blocked list
            _blockedIps.TryRemove(normalizedIp, out _);
        }

        // Check per-IP rate limit
        var windowStart = now.AddMinutes(-_settings.TrackingWindowMinutes);

        var ipRequests = _requestsPerIp.GetOrAdd(normalizedIp, _ => new ConcurrentQueue<DateTime>());
        var recentIpRequests = CountRecentRequests(ipRequests, windowStart);

        if (recentIpRequests >= _settings.MaxRequestsPerMinutePerIp)
        {
            if (_settings.LogViolations)
            {
                _logger.LogWarning(
                    "Rate limit exceeded: IP {IpAddress} has made {Count} requests in the last {Window} minute(s). Limit: {Limit}",
                    normalizedIp, recentIpRequests, _settings.TrackingWindowMinutes, _settings.MaxRequestsPerMinutePerIp);
            }
            return (RateLimitResult.IpRateLimitExceeded, 60);
        }

        // Check total rate limit
        var recentTotalRequests = CountRecentRequests(_totalRequests, windowStart);

        if (recentTotalRequests >= _settings.MaxTotalRequestsPerMinute)
        {
            if (_settings.LogViolations)
            {
                _logger.LogWarning(
                    "Total rate limit exceeded: {Count} requests in the last {Window} minute(s). Limit: {Limit}",
                    recentTotalRequests, _settings.TrackingWindowMinutes, _settings.MaxTotalRequestsPerMinute);
            }
            return (RateLimitResult.TotalRateLimitExceeded, 60);
        }

        // Request is allowed - record it
        ipRequests.Enqueue(now);
        _totalRequests.Enqueue(now);

        return (RateLimitResult.Allowed, null);
    }

    /// <summary>
    /// Records a failed authentication attempt for the given IP.
    /// If the threshold is exceeded, applies exponential backoff.
    /// </summary>
    /// <param name="ipAddress">The IP address of the client.</param>
    public void RecordFailedAttempt(string ipAddress)
    {
        if (!_settings.Enabled)
            return;

        var normalizedIp = NormalizeIpAddress(ipAddress);

        // Whitelisted IPs don't get blocked
        if (_whitelistedIps.Contains(normalizedIp))
            return;

        var now = DateTime.UtcNow;

        var failedInfo = _failedAttempts.AddOrUpdate(
            normalizedIp,
            _ => new FailedAttemptInfo { Count = 1, LastAttempt = now, BackoffLevel = 0 },
            (_, existing) =>
            {
                // Check if we should reset the counter due to time elapsed
                if ((now - existing.LastAttempt).TotalMinutes > _settings.FailedAttemptResetMinutes)
                {
                    return new FailedAttemptInfo { Count = 1, LastAttempt = now, BackoffLevel = 0 };
                }

                existing.Count++;
                existing.LastAttempt = now;
                return existing;
            });

        // Check if we should apply backoff
        if (failedInfo.Count >= _settings.FailedAttemptsBeforeBackoff)
        {
            ApplyExponentialBackoff(normalizedIp, failedInfo);
        }

        if (_settings.LogViolations)
        {
            _logger.LogWarning(
                "Failed authentication attempt from IP {IpAddress}. Total attempts: {Count}",
                normalizedIp, failedInfo.Count);
        }
    }

    /// <summary>
    /// Records a successful authentication for the given IP.
    /// Resets the failed attempt counter for that IP.
    /// </summary>
    /// <param name="ipAddress">The IP address of the client.</param>
    public void RecordSuccessfulAuth(string ipAddress)
    {
        if (!_settings.Enabled)
            return;

        var normalizedIp = NormalizeIpAddress(ipAddress);

        // Reset failed attempts on successful auth
        _failedAttempts.TryRemove(normalizedIp, out _);
    }

    /// <summary>
    /// Gets statistics about the current rate limiter state.
    /// </summary>
    public RateLimiterStats GetStats()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-_settings.TrackingWindowMinutes);

        return new RateLimiterStats
        {
            TotalTrackedIps = _requestsPerIp.Count,
            TotalBlockedIps = _blockedIps.Count,
            TotalFailedAttemptIps = _failedAttempts.Count,
            RecentTotalRequests = CountRecentRequests(_totalRequests, windowStart),
            IsEnabled = _settings.Enabled
        };
    }

    private void ApplyExponentialBackoff(string ip, FailedAttemptInfo failedInfo)
    {
        // Calculate backoff duration with exponential increase
        var backoffSeconds = _settings.InitialBackoffSeconds *
                            Math.Pow(_settings.BackoffMultiplier, failedInfo.BackoffLevel);

        // Cap at maximum
        backoffSeconds = Math.Min(backoffSeconds, _settings.MaxBackoffSeconds);

        var unblockTime = DateTime.UtcNow.AddSeconds(backoffSeconds);

        _blockedIps.AddOrUpdate(ip, unblockTime, (_, _) => unblockTime);

        // Increment backoff level for next time
        failedInfo.BackoffLevel++;

        _logger.LogWarning(
            "IP {IpAddress} blocked for {Seconds} seconds due to {Count} failed attempts (backoff level {Level})",
            ip, (int)backoffSeconds, failedInfo.Count, failedInfo.BackoffLevel);
    }

    private static int CountRecentRequests(ConcurrentQueue<DateTime> requests, DateTime windowStart)
    {
        return requests.Count(r => r >= windowStart);
    }

    private static string NormalizeIpAddress(string ipAddress)
    {
        // Extract IP from endpoint format (e.g., "192.168.1.1:12345" -> "192.168.1.1")
        if (string.IsNullOrEmpty(ipAddress))
            return "unknown";

        // Try to parse as IPEndPoint first
        var colonIndex = ipAddress.LastIndexOf(':');
        if (colonIndex > 0)
        {
            // Check if this is IPv6 with brackets
            if (ipAddress.StartsWith("["))
            {
                var bracketEnd = ipAddress.IndexOf(']');
                if (bracketEnd > 0)
                {
                    return ipAddress.Substring(1, bracketEnd - 1);
                }
            }

            // For IPv4, just take the part before the last colon (port)
            // Be careful with IPv6 addresses
            var potentialIp = ipAddress.Substring(0, colonIndex);
            if (IPAddress.TryParse(potentialIp, out _))
            {
                return potentialIp;
            }
        }

        // Try to parse as-is
        if (IPAddress.TryParse(ipAddress, out var parsed))
        {
            return parsed.ToString();
        }

        return ipAddress;
    }

    private void CleanupExpiredData(object? state)
    {
        if (_disposed) return;

        lock (_cleanupLock)
        {
            if (_disposed) return;

            try
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-_settings.TrackingWindowMinutes);
                var failedAttemptThreshold = now.AddMinutes(-_settings.FailedAttemptResetMinutes);

                // Clean up per-IP request queues
                foreach (var kvp in _requestsPerIp)
                {
                    var queue = kvp.Value;
                    while (queue.TryPeek(out var timestamp) && timestamp < windowStart)
                    {
                        queue.TryDequeue(out _);
                    }

                    // Remove empty queues
                    if (queue.IsEmpty)
                    {
                        _requestsPerIp.TryRemove(kvp.Key, out _);
                    }
                }

                // Clean up total requests queue
                while (_totalRequests.TryPeek(out var timestamp) && timestamp < windowStart)
                {
                    _totalRequests.TryDequeue(out _);
                }

                // Clean up expired blocked IPs
                foreach (var kvp in _blockedIps)
                {
                    if (kvp.Value < now)
                    {
                        _blockedIps.TryRemove(kvp.Key, out _);
                    }
                }

                // Clean up old failed attempts
                foreach (var kvp in _failedAttempts)
                {
                    if (kvp.Value.LastAttempt < failedAttemptThreshold)
                    {
                        _failedAttempts.TryRemove(kvp.Key, out _);
                    }
                }

                _logger.LogDebug(
                    "Rate limiter cleanup completed. Tracked IPs: {IpCount}, Blocked IPs: {BlockedCount}",
                    _requestsPerIp.Count, _blockedIps.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rate limiter cleanup");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cleanupTimer.Dispose();
    }

    /// <summary>
    /// Tracks failed authentication attempts for an IP.
    /// </summary>
    private class FailedAttemptInfo
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; }
        public int BackoffLevel { get; set; }
    }
}

/// <summary>
/// Statistics about the rate limiter state.
/// </summary>
public class RateLimiterStats
{
    public int TotalTrackedIps { get; set; }
    public int TotalBlockedIps { get; set; }
    public int TotalFailedAttemptIps { get; set; }
    public int RecentTotalRequests { get; set; }
    public bool IsEnabled { get; set; }
}
