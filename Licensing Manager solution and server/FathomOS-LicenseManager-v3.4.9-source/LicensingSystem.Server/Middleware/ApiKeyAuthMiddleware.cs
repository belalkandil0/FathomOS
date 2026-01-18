// LicensingSystem.Server/Middleware/ApiKeyAuthMiddleware.cs
// API key authentication middleware for admin endpoints
// Replaces the old username/password admin auth system
// Enhanced with rate limiting per API key and usage tracking

using LicensingSystem.Server.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LicensingSystem.Server.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    // Header name for API key
    private const string ApiKeyHeaderName = "X-API-Key";

    // Rate limiting configuration
    private const int MaxRequestsPerMinute = 100;
    private const int MaxRequestsPerHour = 1000;

    // Rate limiting storage per API key
    private static readonly ConcurrentDictionary<string, ApiKeyRateLimit> _rateLimits = new();
    private static readonly ConcurrentDictionary<string, ApiKeyUsageStats> _usageStats = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;

    // Paths that require API key authentication (admin endpoints)
    private static readonly string[] ProtectedPathPrefixes = new[]
    {
        "/api/admin",
        "/api/analytics"
    };

    // Paths that are always public (no API key needed)
    private static readonly string[] PublicPaths = new[]
    {
        // Root and health
        "/",
        "/health",
        "/db-status",
        "/favicon.ico",

        // Public health endpoints
        "/api/health",
        "/api/health/ready",
        "/api/health/live",

        // Public certificate verification
        "/api/certificates/verify",

        // Public license endpoints (for FathomOS client)
        "/api/license",

        // Public branding endpoint
        "/api/branding/logo",

        // Public module registration
        "/api/modules/register",

        // Swagger
        "/swagger",

        // Portal
        "/portal"
    };

    // Static file extensions
    private static readonly string[] AllowedExtensions = new[]
    {
        ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
        ".woff", ".woff2", ".ttf", ".html", ".map"
    };

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Periodic cleanup of old rate limit entries
        CleanupIfNeeded();

        // Check if path is public
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Check if path requires authentication
        if (!RequiresAuthentication(path))
        {
            await _next(context);
            return;
        }

        // Extract API key from header
        var apiKey = context.Request.Headers[ApiKeyHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("API key missing for protected endpoint: {Path}", path);
            await WriteUnauthorizedResponse(context, "API key required. Include X-API-Key header.");
            return;
        }

        // Check rate limiting before validating the key
        var keyHash = GetKeyHash(apiKey);
        var rateLimitResult = CheckRateLimit(keyHash);

        if (!rateLimitResult.Allowed)
        {
            _logger.LogWarning("Rate limit exceeded for API key hash {KeyHash}: {Reason}", keyHash[..8], rateLimitResult.Reason);
            await WriteRateLimitResponse(context, rateLimitResult.RetryAfterSeconds);
            return;
        }

        // Validate API key
        var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);

        if (!isValid)
        {
            _logger.LogWarning("Invalid API key attempted for: {Path}", path);
            // Still track invalid attempts
            TrackUsage(keyHash, path, false);
            await WriteUnauthorizedResponse(context, "Invalid API key.");
            return;
        }

        // Track successful usage
        TrackUsage(keyHash, path, true);

        // Store key hash in context for downstream services
        context.Items["ApiKeyHash"] = keyHash;

        // API key is valid - proceed
        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        // Check exact paths
        foreach (var publicPath in PublicPaths)
        {
            if (path.Equals(publicPath, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check static file extensions
        foreach (var ext in AllowedExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RequiresAuthentication(string path)
    {
        foreach (var prefix in ProtectedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string GetKeyHash(string apiKey)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static RateLimitResult CheckRateLimit(string keyHash)
    {
        var now = DateTime.UtcNow;

        var rateLimit = _rateLimits.GetOrAdd(keyHash, _ => new ApiKeyRateLimit
        {
            MinuteWindowStart = now,
            HourWindowStart = now,
            MinuteRequests = 0,
            HourRequests = 0
        });

        lock (rateLimit)
        {
            // Reset minute window if expired
            if ((now - rateLimit.MinuteWindowStart).TotalMinutes >= 1)
            {
                rateLimit.MinuteWindowStart = now;
                rateLimit.MinuteRequests = 0;
            }

            // Reset hour window if expired
            if ((now - rateLimit.HourWindowStart).TotalHours >= 1)
            {
                rateLimit.HourWindowStart = now;
                rateLimit.HourRequests = 0;
            }

            // Check minute limit
            if (rateLimit.MinuteRequests >= MaxRequestsPerMinute)
            {
                var retryAfter = (int)(60 - (now - rateLimit.MinuteWindowStart).TotalSeconds);
                return new RateLimitResult
                {
                    Allowed = false,
                    Reason = "Per-minute rate limit exceeded",
                    RetryAfterSeconds = Math.Max(1, retryAfter)
                };
            }

            // Check hour limit
            if (rateLimit.HourRequests >= MaxRequestsPerHour)
            {
                var retryAfter = (int)(3600 - (now - rateLimit.HourWindowStart).TotalSeconds);
                return new RateLimitResult
                {
                    Allowed = false,
                    Reason = "Per-hour rate limit exceeded",
                    RetryAfterSeconds = Math.Max(1, retryAfter)
                };
            }

            // Increment counters
            rateLimit.MinuteRequests++;
            rateLimit.HourRequests++;

            return new RateLimitResult { Allowed = true };
        }
    }

    private static void TrackUsage(string keyHash, string path, bool success)
    {
        var stats = _usageStats.GetOrAdd(keyHash, _ => new ApiKeyUsageStats
        {
            FirstUsedAt = DateTime.UtcNow
        });

        lock (stats)
        {
            stats.TotalRequests++;
            stats.LastUsedAt = DateTime.UtcNow;

            if (success)
            {
                stats.SuccessfulRequests++;
            }
            else
            {
                stats.FailedRequests++;
            }

            // Track endpoint usage
            if (!stats.EndpointCounts.ContainsKey(path))
            {
                stats.EndpointCounts[path] = 0;
            }
            stats.EndpointCounts[path]++;
        }
    }

    private static void CleanupIfNeeded()
    {
        var now = DateTime.UtcNow;

        // Cleanup every 10 minutes
        if ((now - _lastCleanup).TotalMinutes < 10)
            return;

        _lastCleanup = now;

        // Remove rate limit entries older than 2 hours
        var cutoff = now.AddHours(-2);
        var keysToRemove = _rateLimits
            .Where(kvp => kvp.Value.HourWindowStart < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimits.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Gets usage statistics for a specific API key hash.
    /// </summary>
    public static ApiKeyUsageStats? GetUsageStats(string keyHash)
    {
        return _usageStats.TryGetValue(keyHash, out var stats) ? stats : null;
    }

    /// <summary>
    /// Gets all usage statistics.
    /// </summary>
    public static Dictionary<string, ApiKeyUsageStats> GetAllUsageStats()
    {
        return _usageStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Gets current rate limit status for a key.
    /// </summary>
    public static ApiKeyRateLimit? GetRateLimitStatus(string keyHash)
    {
        return _rateLimits.TryGetValue(keyHash, out var limit) ? limit : null;
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "unauthorized",
            message = message,
            hint = "Include a valid API key in the X-API-Key header"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static async Task WriteRateLimitResponse(HttpContext context, int retryAfterSeconds)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        context.Response.Headers["X-RateLimit-Limit"] = MaxRequestsPerMinute.ToString();

        var response = new
        {
            error = "rate_limit_exceeded",
            message = "Too many requests. Please slow down.",
            retryAfterSeconds,
            limits = new
            {
                perMinute = MaxRequestsPerMinute,
                perHour = MaxRequestsPerHour
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

/// <summary>
/// Rate limiting tracking per API key.
/// </summary>
public class ApiKeyRateLimit
{
    public DateTime MinuteWindowStart { get; set; }
    public DateTime HourWindowStart { get; set; }
    public int MinuteRequests { get; set; }
    public int HourRequests { get; set; }
}

/// <summary>
/// Usage statistics per API key.
/// </summary>
public class ApiKeyUsageStats
{
    public DateTime FirstUsedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public Dictionary<string, long> EndpointCounts { get; set; } = new();
}

/// <summary>
/// Result of rate limit check.
/// </summary>
public class RateLimitResult
{
    public bool Allowed { get; set; }
    public string? Reason { get; set; }
    public int RetryAfterSeconds { get; set; }
}

// Extension method for easy registration
public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
