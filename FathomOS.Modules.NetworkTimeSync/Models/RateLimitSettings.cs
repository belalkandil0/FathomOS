// ============================================================================
// Fathom OS - Network Time Sync Module
// File: Models/RateLimitSettings.cs
// Purpose: Configuration settings for rate limiting (MISSING-007)
// Version: 1.0 - Initial implementation
// ============================================================================
//
// SECURITY NOTES:
// - Provides configurable thresholds for rate limiting
// - Default values follow security best practices
// - Supports whitelisting for trusted IPs
// - Exponential backoff protects against brute-force attacks
//
// ============================================================================

namespace FathomOS.Modules.NetworkTimeSync.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

/// <summary>
/// Configuration settings for rate limiting in the NetworkTimeSync module.
/// Provides protection against brute-force attacks and denial-of-service attempts.
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Whether rate limiting is enabled.
    /// Default: true for security.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute allowed from a single IP address.
    /// Default: 10 (as per MISSING-007 requirements).
    /// </summary>
    public int MaxRequestsPerMinutePerIp { get; set; } = 10;

    /// <summary>
    /// Maximum total requests per minute allowed across all IPs.
    /// Default: 100 (as per MISSING-007 requirements).
    /// </summary>
    public int MaxTotalRequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Number of failed authentication attempts before triggering exponential backoff.
    /// Default: 5 (as per MISSING-007 requirements).
    /// </summary>
    public int FailedAttemptsBeforeBackoff { get; set; } = 5;

    /// <summary>
    /// Initial backoff duration in seconds after exceeding failed attempt threshold.
    /// Default: 30 seconds.
    /// </summary>
    public int InitialBackoffSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum backoff duration in seconds (caps exponential growth).
    /// Default: 3600 seconds (1 hour).
    /// </summary>
    public int MaxBackoffSeconds { get; set; } = 3600;

    /// <summary>
    /// Multiplier for exponential backoff calculation.
    /// Default: 2.0 (double each time).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Time window in minutes for tracking requests (sliding window).
    /// Default: 1 minute.
    /// </summary>
    public int TrackingWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Time in minutes after which an IP's failed attempt counter resets if no new failures occur.
    /// Default: 60 minutes.
    /// </summary>
    public int FailedAttemptResetMinutes { get; set; } = 60;

    /// <summary>
    /// Comma-separated list of IP addresses that are exempt from rate limiting (whitelist).
    /// Default: "127.0.0.1,::1" (localhost).
    /// </summary>
    public string WhitelistedIps { get; set; } = "127.0.0.1,::1";

    /// <summary>
    /// Comma-separated list of IP addresses that are always blocked (blacklist).
    /// Default: empty.
    /// </summary>
    public string BlacklistedIps { get; set; } = string.Empty;

    /// <summary>
    /// Whether to log rate limit violations.
    /// Default: true for security monitoring.
    /// </summary>
    public bool LogViolations { get; set; } = true;

    /// <summary>
    /// Interval in minutes between cleanup operations for expired tracking data.
    /// Default: 5 minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum number of IPs to track (prevents memory exhaustion attacks).
    /// Default: 1000.
    /// </summary>
    public int MaxTrackedIps { get; set; } = 1000;

    /// <summary>
    /// Gets the whitelisted IPs as a collection.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<string> WhitelistedIpList => ParseIpList(WhitelistedIps);

    /// <summary>
    /// Gets the blacklisted IPs as a collection.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<string> BlacklistedIpList => ParseIpList(BlacklistedIps);

    /// <summary>
    /// Gets the whitelisted IPs as a collection.
    /// </summary>
    public IEnumerable<string> GetWhitelistedIps() => WhitelistedIpList;

    /// <summary>
    /// Gets the blacklisted IPs as a collection.
    /// </summary>
    public IEnumerable<string> GetBlacklistedIps() => BlacklistedIpList;

    /// <summary>
    /// Adds an IP address to the whitelist.
    /// </summary>
    /// <param name="ipAddress">The IP address to whitelist.</param>
    public void AddToWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return;

        var ips = new HashSet<string>(WhitelistedIpList, StringComparer.OrdinalIgnoreCase);
        ips.Add(ipAddress.Trim());
        WhitelistedIps = string.Join(",", ips);

        // Remove from blacklist if present
        RemoveFromBlacklist(ipAddress);
    }

    /// <summary>
    /// Removes an IP address from the whitelist.
    /// </summary>
    /// <param name="ipAddress">The IP address to remove.</param>
    public void RemoveFromWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return;

        var ips = new HashSet<string>(WhitelistedIpList, StringComparer.OrdinalIgnoreCase);
        ips.Remove(ipAddress.Trim());
        WhitelistedIps = string.Join(",", ips);
    }

    /// <summary>
    /// Adds an IP address to the blacklist.
    /// </summary>
    /// <param name="ipAddress">The IP address to blacklist.</param>
    public void AddToBlacklist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return;

        var ips = new HashSet<string>(BlacklistedIpList, StringComparer.OrdinalIgnoreCase);
        ips.Add(ipAddress.Trim());
        BlacklistedIps = string.Join(",", ips);

        // Remove from whitelist if present
        RemoveFromWhitelist(ipAddress);
    }

    /// <summary>
    /// Removes an IP address from the blacklist.
    /// </summary>
    /// <param name="ipAddress">The IP address to remove.</param>
    public void RemoveFromBlacklist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return;

        var ips = new HashSet<string>(BlacklistedIpList, StringComparer.OrdinalIgnoreCase);
        ips.Remove(ipAddress.Trim());
        BlacklistedIps = string.Join(",", ips);
    }

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    /// <returns>A list of validation errors, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (MaxRequestsPerMinutePerIp <= 0)
            errors.Add("MaxRequestsPerMinutePerIp must be greater than 0");

        if (MaxTotalRequestsPerMinute <= 0)
            errors.Add("MaxTotalRequestsPerMinute must be greater than 0");

        if (MaxTotalRequestsPerMinute < MaxRequestsPerMinutePerIp)
            errors.Add("MaxTotalRequestsPerMinute should be greater than or equal to MaxRequestsPerMinutePerIp");

        if (FailedAttemptsBeforeBackoff <= 0)
            errors.Add("FailedAttemptsBeforeBackoff must be greater than 0");

        if (InitialBackoffSeconds <= 0)
            errors.Add("InitialBackoffSeconds must be greater than 0");

        if (MaxBackoffSeconds < InitialBackoffSeconds)
            errors.Add("MaxBackoffSeconds must be greater than or equal to InitialBackoffSeconds");

        if (BackoffMultiplier <= 1.0)
            errors.Add("BackoffMultiplier must be greater than 1.0");

        if (TrackingWindowMinutes <= 0)
            errors.Add("TrackingWindowMinutes must be greater than 0");

        if (CleanupIntervalMinutes <= 0)
            errors.Add("CleanupIntervalMinutes must be greater than 0");

        if (MaxTrackedIps <= 0)
            errors.Add("MaxTrackedIps must be greater than 0");

        return errors;
    }

    /// <summary>
    /// Creates a copy of the settings with default values.
    /// </summary>
    public static RateLimitSettings CreateDefault() => new();

    /// <summary>
    /// Creates settings optimized for high-security environments.
    /// </summary>
    public static RateLimitSettings CreateHighSecurity() => new()
    {
        MaxRequestsPerMinutePerIp = 5,
        MaxTotalRequestsPerMinute = 50,
        FailedAttemptsBeforeBackoff = 3,
        InitialBackoffSeconds = 60,
        MaxBackoffSeconds = 7200,
        BackoffMultiplier = 3.0,
        LogViolations = true
    };

    /// <summary>
    /// Creates settings optimized for development/testing environments.
    /// </summary>
    public static RateLimitSettings CreateDevelopment() => new()
    {
        MaxRequestsPerMinutePerIp = 100,
        MaxTotalRequestsPerMinute = 1000,
        FailedAttemptsBeforeBackoff = 10,
        InitialBackoffSeconds = 10,
        MaxBackoffSeconds = 300,
        LogViolations = true
    };

    private static IEnumerable<string> ParseIpList(string? ipList)
    {
        if (string.IsNullOrWhiteSpace(ipList))
            return Enumerable.Empty<string>();

        return ipList
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ip => ip.Trim())
            .Where(ip => !string.IsNullOrEmpty(ip));
    }
}
