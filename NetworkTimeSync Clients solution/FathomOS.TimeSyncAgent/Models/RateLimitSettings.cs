namespace FathomOS.TimeSyncAgent.Models;

/// <summary>
/// Configuration settings for rate limiting in the TCP listener service.
/// Provides protection against brute-force attacks and denial-of-service attempts.
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Whether rate limiting is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute allowed from a single IP address.
    /// Default: 10
    /// </summary>
    public int MaxRequestsPerMinutePerIp { get; set; } = 10;

    /// <summary>
    /// Maximum total requests per minute allowed across all IPs.
    /// Default: 100
    /// </summary>
    public int MaxTotalRequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Number of failed authentication attempts before triggering exponential backoff.
    /// Default: 5
    /// </summary>
    public int FailedAttemptsBeforeBackoff { get; set; } = 5;

    /// <summary>
    /// Initial backoff duration in seconds after exceeding failed attempt threshold.
    /// Default: 30 seconds
    /// </summary>
    public int InitialBackoffSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum backoff duration in seconds (caps exponential growth).
    /// Default: 3600 seconds (1 hour)
    /// </summary>
    public int MaxBackoffSeconds { get; set; } = 3600;

    /// <summary>
    /// Multiplier for exponential backoff calculation.
    /// Default: 2 (double each time)
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Time window in minutes for tracking requests (sliding window).
    /// Default: 1 minute
    /// </summary>
    public int TrackingWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Time in minutes after which an IP's failed attempt counter resets if no new failures occur.
    /// Default: 60 minutes
    /// </summary>
    public int FailedAttemptResetMinutes { get; set; } = 60;

    /// <summary>
    /// Comma-separated list of IP addresses that are exempt from rate limiting (whitelist).
    /// Example: "127.0.0.1,192.168.1.100"
    /// Default: "127.0.0.1,::1" (localhost)
    /// </summary>
    public string WhitelistedIps { get; set; } = "127.0.0.1,::1";

    /// <summary>
    /// Whether to log rate limit violations.
    /// Default: true
    /// </summary>
    public bool LogViolations { get; set; } = true;

    /// <summary>
    /// Interval in minutes between cleanup operations for expired tracking data.
    /// Default: 5 minutes
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets the whitelisted IPs as a collection.
    /// </summary>
    public IEnumerable<string> GetWhitelistedIps()
    {
        if (string.IsNullOrWhiteSpace(WhitelistedIps))
            return Enumerable.Empty<string>();

        return WhitelistedIps
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ip => ip.Trim())
            .Where(ip => !string.IsNullOrEmpty(ip));
    }
}
