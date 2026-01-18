// ============================================================================
// Fathom OS - Network Time Sync Module
// File: Services/RateLimiter.cs
// Purpose: Rate limiting service for time sync connections (MISSING-007)
// Version: 1.0 - Initial implementation
// ============================================================================
//
// SECURITY NOTES:
// - Implements sliding window algorithm for request rate limiting
// - Per-IP connection throttling prevents single-source abuse
// - Exponential backoff protects against brute-force authentication attacks
// - Configurable thresholds support different deployment scenarios
// - Thread-safe implementation using ConcurrentDictionary
// - Automatic cleanup prevents memory exhaustion
//
// ============================================================================

namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Result of a rate limit check.
/// </summary>
public enum RateLimitCheckResult
{
    /// <summary>Request is allowed.</summary>
    Allowed,

    /// <summary>Blocked due to per-IP rate limit exceeded.</summary>
    IpRateLimitExceeded,

    /// <summary>Blocked due to total rate limit exceeded.</summary>
    TotalRateLimitExceeded,

    /// <summary>Blocked due to exponential backoff from failed attempts.</summary>
    BlockedByBackoff,

    /// <summary>Blocked because IP is blacklisted.</summary>
    Blacklisted,

    /// <summary>Rate limiting is disabled.</summary>
    Disabled
}

/// <summary>
/// Detailed result of a rate limit check.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// The check result status.
    /// </summary>
    public RateLimitCheckResult Status { get; init; }

    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed => Status == RateLimitCheckResult.Allowed || Status == RateLimitCheckResult.Disabled;

    /// <summary>
    /// Retry after this many seconds (if rate limited).
    /// </summary>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>
    /// Human-readable message describing the result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Remaining requests in the current window (if allowed).
    /// </summary>
    public int? RemainingRequests { get; init; }

    public static RateLimitResult Allow(int remaining) => new()
    {
        Status = RateLimitCheckResult.Allowed,
        Message = "Request allowed",
        RemainingRequests = remaining
    };

    public static RateLimitResult Disabled() => new()
    {
        Status = RateLimitCheckResult.Disabled,
        Message = "Rate limiting is disabled"
    };

    public static RateLimitResult IpLimitExceeded(int retryAfter) => new()
    {
        Status = RateLimitCheckResult.IpRateLimitExceeded,
        Message = "Too many requests from your IP address",
        RetryAfterSeconds = retryAfter
    };

    public static RateLimitResult TotalLimitExceeded(int retryAfter) => new()
    {
        Status = RateLimitCheckResult.TotalRateLimitExceeded,
        Message = "Server is experiencing high load",
        RetryAfterSeconds = retryAfter
    };

    public static RateLimitResult BackoffBlocked(int retryAfter) => new()
    {
        Status = RateLimitCheckResult.BlockedByBackoff,
        Message = "Too many failed authentication attempts",
        RetryAfterSeconds = retryAfter
    };

    public static RateLimitResult BlacklistedIp() => new()
    {
        Status = RateLimitCheckResult.Blacklisted,
        Message = "IP address is blocked"
    };
}

/// <summary>
/// Statistics about the rate limiter state.
/// </summary>
public class RateLimiterStatistics
{
    /// <summary>Total number of IP addresses being tracked.</summary>
    public int TotalTrackedIps { get; set; }

    /// <summary>Number of IP addresses currently blocked.</summary>
    public int TotalBlockedIps { get; set; }

    /// <summary>Number of IP addresses with failed authentication attempts.</summary>
    public int TotalFailedAttemptIps { get; set; }

    /// <summary>Total requests in the current tracking window.</summary>
    public int RecentTotalRequests { get; set; }

    /// <summary>Whether rate limiting is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Number of rate limit violations in current session.</summary>
    public long TotalViolations { get; set; }

    /// <summary>Number of successful authentication resets.</summary>
    public long TotalSuccessfulAuths { get; set; }
}

/// <summary>
/// Event arguments for rate limit violation events.
/// </summary>
public class RateLimitViolationEventArgs : EventArgs
{
    /// <summary>IP address that triggered the violation.</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>Type of violation.</summary>
    public RateLimitCheckResult ViolationType { get; init; }

    /// <summary>Human-readable description of the violation.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Timestamp when the violation occurred.</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Number of previous violations from this IP.</summary>
    public int ViolationCount { get; init; }

    /// <summary>Whether this violation resulted in a block.</summary>
    public bool WasBlocked { get; init; }
}

/// <summary>
/// Tracks failed authentication attempts for an IP.
/// </summary>
internal class FailedAttemptInfo
{
    public int Count { get; set; }
    public DateTime LastAttempt { get; set; }
    public int BackoffLevel { get; set; }
}

/// <summary>
/// Rate limiter for the NetworkTimeSync module.
/// Provides per-IP connection throttling and brute-force protection with exponential backoff.
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly RateLimitSettings _settings;
    private readonly HashSet<string> _whitelistedIps;
    private readonly HashSet<string> _blacklistedIps;

    // Track requests per IP (IP -> list of request timestamps)
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requestsPerIp = new();

    // Track total requests (list of all request timestamps)
    private readonly ConcurrentQueue<DateTime> _totalRequests = new();

    // Track failed authentication attempts per IP
    private readonly ConcurrentDictionary<string, FailedAttemptInfo> _failedAttempts = new();

    // Track blocked IPs with their unblock time
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();

    // Statistics
    private long _totalViolations;
    private long _totalSuccessfulAuths;

    // Cleanup timer
    private readonly Timer _cleanupTimer;
    private readonly object _cleanupLock = new();
    private bool _disposed;

    // Logging
    private readonly string _logPath;
    private StreamWriter? _logWriter;
    private readonly object _logLock = new();

    /// <summary>
    /// Event raised when a rate limit violation occurs.
    /// </summary>
    public event EventHandler<RateLimitViolationEventArgs>? ViolationOccurred;

    /// <summary>
    /// Initializes a new rate limiter with the specified settings.
    /// </summary>
    /// <param name="settings">Rate limit configuration settings.</param>
    public RateLimiter(RateLimitSettings? settings = null)
    {
        _settings = settings ?? new RateLimitSettings();
        _whitelistedIps = new HashSet<string>(_settings.GetWhitelistedIps(), StringComparer.OrdinalIgnoreCase);
        _blacklistedIps = new HashSet<string>(_settings.GetBlacklistedIps(), StringComparer.OrdinalIgnoreCase);

        // Start cleanup timer
        var cleanupInterval = TimeSpan.FromMinutes(_settings.CleanupIntervalMinutes);
        _cleanupTimer = new Timer(CleanupExpiredData, null, cleanupInterval, cleanupInterval);

        // Setup log file
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "NetworkTimeSync", "Logs");
        Directory.CreateDirectory(logFolder);
        _logPath = Path.Combine(logFolder, $"RateLimit_{DateTime.Now:yyyyMMdd}.log");

        if (_settings.LogViolations)
        {
            try
            {
                _logWriter = new StreamWriter(_logPath, append: true) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RateLimiter] Failed to open log file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Checks if a request from the given IP is allowed based on rate limits.
    /// </summary>
    /// <param name="ipAddress">The IP address of the client.</param>
    /// <returns>The rate limit result.</returns>
    public RateLimitResult CheckRateLimit(string ipAddress)
    {
        if (!_settings.Enabled)
            return RateLimitResult.Disabled();

        var normalizedIp = NormalizeIpAddress(ipAddress);

        // Check blacklist first
        if (_blacklistedIps.Contains(normalizedIp))
        {
            LogViolation(normalizedIp, RateLimitCheckResult.Blacklisted, "Blacklisted IP attempted connection", 0, false);
            return RateLimitResult.BlacklistedIp();
        }

        // Check whitelist
        if (_whitelistedIps.Contains(normalizedIp))
            return RateLimitResult.Allow(int.MaxValue);

        var now = DateTime.UtcNow;

        // Check if IP is blocked due to backoff
        if (_blockedIps.TryGetValue(normalizedIp, out var unblockTime))
        {
            if (now < unblockTime)
            {
                var retryAfter = (int)Math.Ceiling((unblockTime - now).TotalSeconds);
                LogViolation(normalizedIp, RateLimitCheckResult.BlockedByBackoff,
                    $"IP blocked due to backoff. Retry after {retryAfter} seconds", 0, true);
                return RateLimitResult.BackoffBlocked(retryAfter);
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
            var retryAfter = CalculateRetryAfterSeconds(ipRequests, windowStart);
            LogViolation(normalizedIp, RateLimitCheckResult.IpRateLimitExceeded,
                $"IP rate limit exceeded: {recentIpRequests}/{_settings.MaxRequestsPerMinutePerIp} requests",
                0, false);
            return RateLimitResult.IpLimitExceeded(retryAfter);
        }

        // Check total rate limit
        var recentTotalRequests = CountRecentRequests(_totalRequests, windowStart);

        if (recentTotalRequests >= _settings.MaxTotalRequestsPerMinute)
        {
            var retryAfter = CalculateRetryAfterSeconds(_totalRequests, windowStart);
            LogViolation(normalizedIp, RateLimitCheckResult.TotalRateLimitExceeded,
                $"Total rate limit exceeded: {recentTotalRequests}/{_settings.MaxTotalRequestsPerMinute} requests",
                0, false);
            return RateLimitResult.TotalLimitExceeded(retryAfter);
        }

        // Request is allowed - record it
        ipRequests.Enqueue(now);
        _totalRequests.Enqueue(now);

        var remaining = _settings.MaxRequestsPerMinutePerIp - recentIpRequests - 1;
        return RateLimitResult.Allow(Math.Max(0, remaining));
    }

    /// <summary>
    /// Checks if a request from the given endpoint is allowed.
    /// </summary>
    /// <param name="endpoint">The IP endpoint of the client.</param>
    /// <returns>The rate limit result.</returns>
    public RateLimitResult CheckRateLimit(IPEndPoint? endpoint)
    {
        if (endpoint == null)
            return RateLimitResult.Allow(int.MaxValue);

        return CheckRateLimit(endpoint.Address.ToString());
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
        var wasBlocked = false;
        if (failedInfo.Count >= _settings.FailedAttemptsBeforeBackoff)
        {
            ApplyExponentialBackoff(normalizedIp, failedInfo);
            wasBlocked = true;
        }

        LogViolation(normalizedIp, RateLimitCheckResult.BlockedByBackoff,
            $"Failed authentication attempt #{failedInfo.Count}",
            failedInfo.Count, wasBlocked);
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
        if (_failedAttempts.TryRemove(normalizedIp, out _))
        {
            Interlocked.Increment(ref _totalSuccessfulAuths);
        }
    }

    /// <summary>
    /// Manually blocks an IP address for a specified duration.
    /// </summary>
    /// <param name="ipAddress">IP address to block.</param>
    /// <param name="durationSeconds">Duration of the block in seconds (null = use default).</param>
    public void BlockIp(string ipAddress, int? durationSeconds = null)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);
        var duration = durationSeconds ?? _settings.MaxBackoffSeconds;
        var unblockTime = DateTime.UtcNow.AddSeconds(duration);

        _blockedIps.AddOrUpdate(normalizedIp, unblockTime, (_, _) => unblockTime);

        LogViolation(normalizedIp, RateLimitCheckResult.BlockedByBackoff,
            $"Manually blocked for {duration} seconds", 0, true);
    }

    /// <summary>
    /// Unblocks an IP address.
    /// </summary>
    /// <param name="ipAddress">IP address to unblock.</param>
    public void UnblockIp(string ipAddress)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);
        _blockedIps.TryRemove(normalizedIp, out _);
        _failedAttempts.TryRemove(normalizedIp, out _);

        Debug.WriteLine($"[RateLimiter] Unblocked IP: {normalizedIp}");
    }

    /// <summary>
    /// Adds an IP address to the runtime whitelist.
    /// </summary>
    public void AddToWhitelist(string ipAddress)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);
        _whitelistedIps.Add(normalizedIp);
        _blacklistedIps.Remove(normalizedIp);
        UnblockIp(normalizedIp);
    }

    /// <summary>
    /// Removes an IP address from the runtime whitelist.
    /// </summary>
    public void RemoveFromWhitelist(string ipAddress)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);
        _whitelistedIps.Remove(normalizedIp);
    }

    /// <summary>
    /// Adds an IP address to the runtime blacklist.
    /// </summary>
    public void AddToBlacklist(string ipAddress)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);
        _blacklistedIps.Add(normalizedIp);
        _whitelistedIps.Remove(normalizedIp);
    }

    /// <summary>
    /// Removes an IP address from the runtime blacklist.
    /// </summary>
    public void RemoveFromBlacklist(string ipAddress)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);
        _blacklistedIps.Remove(normalizedIp);
    }

    /// <summary>
    /// Checks if an IP address is currently blocked.
    /// </summary>
    public bool IsBlocked(string ipAddress)
    {
        var normalizedIp = NormalizeIpAddress(ipAddress);

        if (_blacklistedIps.Contains(normalizedIp))
            return true;

        if (_blockedIps.TryGetValue(normalizedIp, out var unblockTime))
            return DateTime.UtcNow < unblockTime;

        return false;
    }

    /// <summary>
    /// Gets statistics about the current rate limiter state.
    /// </summary>
    public RateLimiterStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-_settings.TrackingWindowMinutes);

        return new RateLimiterStatistics
        {
            TotalTrackedIps = _requestsPerIp.Count,
            TotalBlockedIps = _blockedIps.Count(kvp => kvp.Value > now) + _blacklistedIps.Count,
            TotalFailedAttemptIps = _failedAttempts.Count,
            RecentTotalRequests = CountRecentRequests(_totalRequests, windowStart),
            IsEnabled = _settings.Enabled,
            TotalViolations = Interlocked.Read(ref _totalViolations),
            TotalSuccessfulAuths = Interlocked.Read(ref _totalSuccessfulAuths)
        };
    }

    /// <summary>
    /// Gets the current settings (read-only).
    /// </summary>
    public RateLimitSettings Settings => _settings;

    /// <summary>
    /// Resets all tracking data.
    /// </summary>
    public void Reset()
    {
        _requestsPerIp.Clear();
        while (_totalRequests.TryDequeue(out _)) { }
        _failedAttempts.Clear();
        _blockedIps.Clear();
        Interlocked.Exchange(ref _totalViolations, 0);
        Interlocked.Exchange(ref _totalSuccessfulAuths, 0);

        Debug.WriteLine("[RateLimiter] All data reset");
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

        Debug.WriteLine($"[RateLimiter] IP {ip} blocked for {(int)backoffSeconds} seconds " +
                       $"(level {failedInfo.BackoffLevel}, {failedInfo.Count} failed attempts)");
    }

    private static int CountRecentRequests(ConcurrentQueue<DateTime> requests, DateTime windowStart)
    {
        return requests.Count(r => r >= windowStart);
    }

    private int CalculateRetryAfterSeconds(ConcurrentQueue<DateTime> requests, DateTime windowStart)
    {
        var oldestInWindow = requests.Where(r => r >= windowStart).OrderBy(r => r).FirstOrDefault();
        if (oldestInWindow == default)
            return _settings.TrackingWindowMinutes * 60;

        var expiresAt = oldestInWindow.AddMinutes(_settings.TrackingWindowMinutes);
        var retryAfter = (int)Math.Ceiling((expiresAt - DateTime.UtcNow).TotalSeconds);
        return Math.Max(1, retryAfter);
    }

    private static string NormalizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return "unknown";

        // Handle IPv4-mapped IPv6 addresses
        if (ipAddress.StartsWith("::ffff:"))
            return ipAddress.Substring(7);

        // Extract IP from endpoint format (e.g., "192.168.1.1:12345" -> "192.168.1.1")
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

        return ipAddress.Trim();
    }

    private void LogViolation(string ipAddress, RateLimitCheckResult violationType,
        string description, int violationCount, bool wasBlocked)
    {
        Interlocked.Increment(ref _totalViolations);

        var args = new RateLimitViolationEventArgs
        {
            IpAddress = ipAddress,
            ViolationType = violationType,
            Description = description,
            Timestamp = DateTime.Now,
            ViolationCount = violationCount,
            WasBlocked = wasBlocked
        };

        ViolationOccurred?.Invoke(this, args);

        if (_settings.LogViolations)
        {
            var logEntry = $"[{args.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] " +
                          $"IP={ipAddress} | " +
                          $"Type={violationType} | " +
                          $"Description={description} | " +
                          $"ViolationCount={violationCount} | " +
                          $"Blocked={wasBlocked}";

            Debug.WriteLine($"[RateLimiter] {logEntry}");

            lock (_logLock)
            {
                try
                {
                    _logWriter?.WriteLine(logEntry);
                }
                catch
                {
                    // Ignore logging errors
                }
            }
        }
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
                var ipsToRemove = new List<string>();
                foreach (var kvp in _requestsPerIp)
                {
                    var queue = kvp.Value;

                    // Remove old timestamps
                    while (queue.TryPeek(out var timestamp) && timestamp < windowStart)
                    {
                        queue.TryDequeue(out _);
                    }

                    // Mark empty queues for removal
                    if (queue.IsEmpty)
                    {
                        ipsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var ip in ipsToRemove)
                {
                    _requestsPerIp.TryRemove(ip, out _);
                }

                // Clean up total requests queue
                while (_totalRequests.TryPeek(out var timestamp) && timestamp < windowStart)
                {
                    _totalRequests.TryDequeue(out _);
                }

                // Clean up expired blocked IPs
                var blockedToRemove = _blockedIps.Where(kvp => kvp.Value < now).Select(kvp => kvp.Key).ToList();
                foreach (var ip in blockedToRemove)
                {
                    _blockedIps.TryRemove(ip, out _);
                }

                // Clean up old failed attempts
                var failedToRemove = _failedAttempts
                    .Where(kvp => kvp.Value.LastAttempt < failedAttemptThreshold)
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var ip in failedToRemove)
                {
                    _failedAttempts.TryRemove(ip, out _);
                }

                // Enforce max tracked IPs limit
                if (_requestsPerIp.Count > _settings.MaxTrackedIps)
                {
                    var excess = _requestsPerIp.Count - _settings.MaxTrackedIps;
                    var oldestIps = _requestsPerIp
                        .Select(kvp => new { kvp.Key, LastRequest = kvp.Value.OrderByDescending(t => t).FirstOrDefault() })
                        .OrderBy(x => x.LastRequest)
                        .Take(excess)
                        .Select(x => x.Key)
                        .ToList();

                    foreach (var ip in oldestIps)
                    {
                        _requestsPerIp.TryRemove(ip, out _);
                    }

                    Debug.WriteLine($"[RateLimiter] Removed {oldestIps.Count} oldest entries (capacity limit)");
                }

                Debug.WriteLine($"[RateLimiter] Cleanup completed. Tracked IPs: {_requestsPerIp.Count}, " +
                               $"Blocked IPs: {_blockedIps.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RateLimiter] Error during cleanup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Disposes the rate limiter and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_logLock)
        {
            try
            {
                _logWriter?.Close();
                _logWriter?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        GC.SuppressFinalize(this);
    }
}
