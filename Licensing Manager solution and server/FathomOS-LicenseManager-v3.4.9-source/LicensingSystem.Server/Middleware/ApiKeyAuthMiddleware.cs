// LicensingSystem.Server/Middleware/ApiKeyAuthMiddleware.cs
// API key authentication middleware for admin endpoints
// Replaces the old username/password admin auth system

using LicensingSystem.Server.Services;
using System.Text.Json;

namespace LicensingSystem.Server.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    // Header name for API key
    private const string ApiKeyHeaderName = "X-API-Key";

    // Paths that require API key authentication (admin endpoints)
    private static readonly string[] ProtectedPathPrefixes = new[]
    {
        "/api/admin"
    };

    // Paths that are always public (no API key needed)
    private static readonly string[] PublicPaths = new[]
    {
        // Root and health
        "/",
        "/health",
        "/db-status",
        "/favicon.ico",

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

        // Validate API key
        var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);

        if (!isValid)
        {
            _logger.LogWarning("Invalid API key attempted for: {Path}", path);
            await WriteUnauthorizedResponse(context, "Invalid API key.");
            return;
        }

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
}

// Extension method for easy registration
public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
