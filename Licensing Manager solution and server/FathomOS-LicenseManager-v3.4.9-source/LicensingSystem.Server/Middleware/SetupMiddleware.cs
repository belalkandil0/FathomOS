// LicensingSystem.Server/Middleware/SetupMiddleware.cs
// Middleware to detect when first-time setup is required and redirect appropriately

using LicensingSystem.Server.Services;
using System.Text.Json;

namespace LicensingSystem.Server.Middleware;

public class SetupMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupMiddleware> _logger;

    // Paths that are always allowed, even during setup mode
    private static readonly string[] AllowedPaths = new[]
    {
        "/health",
        "/setup",
        "/setup.html",
        "/api/setup",
        "/swagger",
        "/favicon.ico"
    };

    // Static file extensions that should be served
    private static readonly string[] AllowedExtensions = new[]
    {
        ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2", ".ttf"
    };

    public SetupMiddleware(RequestDelegate next, ILogger<SetupMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISetupService setupService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Always allow certain paths
        if (IsAllowedPath(path))
        {
            await _next(context);
            return;
        }

        // Check if setup is required
        try
        {
            var setupRequired = await setupService.IsSetupRequiredAsync();

            if (setupRequired)
            {
                _logger.LogDebug("Setup required, blocking request to {Path}", path);

                // Determine if this is an API request or browser request
                var acceptHeader = context.Request.Headers["Accept"].ToString();
                var isApiRequest = context.Request.Path.StartsWithSegments("/api") ||
                                   acceptHeader.Contains("application/json") ||
                                   !acceptHeader.Contains("text/html");

                if (isApiRequest)
                {
                    // Return 503 Service Unavailable with JSON response for API requests
                    context.Response.StatusCode = 503;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "setup_required",
                        message = "Server setup is not complete. Please complete the initial setup before using the API.",
                        setupUrl = "/setup",
                        instructions = new[]
                        {
                            "1. Navigate to the server's setup page at /setup",
                            "2. Enter the setup token displayed in the server console",
                            "3. Create your administrator account",
                            "4. Optionally configure two-factor authentication"
                        }
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
                    return;
                }
                else
                {
                    // Redirect browser requests to the setup page
                    context.Response.Redirect("/setup");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking setup status");
            // If we can't determine setup status, let the request through
            // The actual endpoints will handle authorization
        }

        await _next(context);
    }

    private static bool IsAllowedPath(string path)
    {
        // Check if path starts with any allowed path
        foreach (var allowedPath in AllowedPaths)
        {
            if (path.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check if it's a static file with an allowed extension
        foreach (var ext in AllowedExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

// Extension method for easy registration
public static class SetupMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SetupMiddleware>();
    }
}
