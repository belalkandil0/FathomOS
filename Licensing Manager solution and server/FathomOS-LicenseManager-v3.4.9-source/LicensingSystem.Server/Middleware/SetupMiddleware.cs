// LicensingSystem.Server/Middleware/SetupMiddleware.cs
// DEPRECATED - This middleware is no longer used.
// The server now uses API key authentication via ApiKeyAuthMiddleware.
// This file is kept for backward compatibility but does nothing.

namespace LicensingSystem.Server.Middleware;

/// <summary>
/// DEPRECATED: This middleware is no longer needed.
/// The server uses API key authentication now - see ApiKeyAuthMiddleware.
/// </summary>
public class SetupMiddleware
{
    private readonly RequestDelegate _next;

    public SetupMiddleware(RequestDelegate next, ILogger<SetupMiddleware> logger)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Pass through - no longer used for setup blocking
        await _next(context);
    }
}

// Extension method kept for backward compatibility
public static class SetupMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupMiddleware(this IApplicationBuilder builder)
    {
        // No longer registers the middleware - use UseApiKeyAuth instead
        return builder;
    }
}
