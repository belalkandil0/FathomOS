// LicensingSystem.Server/Controllers/AdminAuthController.cs
// DEPRECATED - Admin authentication is now handled via API keys
// This controller returns deprecation messages for backward compatibility

using Microsoft.AspNetCore.Mvc;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// DEPRECATED: Admin authentication is now handled via API keys.
/// Use the X-API-Key header instead of username/password login.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(ILogger<AdminAuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// DEPRECATED: Login is no longer required.
    /// Use X-API-Key header for authentication.
    /// </summary>
    [HttpPost("login")]
    public ActionResult Login()
    {
        _logger.LogWarning("Login endpoint called but authentication is now via API keys");

        return Ok(new
        {
            deprecated = true,
            message = "Username/password login is deprecated. Use X-API-Key header for authentication.",
            instructions = new[]
            {
                "1. Get your API key from the server console on first startup",
                "2. Or set the ADMIN_API_KEY environment variable",
                "3. Include the key in the X-API-Key header for all admin requests"
            }
        });
    }

    /// <summary>
    /// DEPRECATED: Sessions are no longer used.
    /// </summary>
    [HttpPost("logout")]
    public ActionResult Logout()
    {
        return Ok(new
        {
            deprecated = true,
            message = "Sessions are deprecated. API key authentication is stateless."
        });
    }

    /// <summary>
    /// DEPRECATED: 2FA is no longer used.
    /// </summary>
    [HttpPost("verify-2fa")]
    public ActionResult Verify2FA()
    {
        return Ok(new
        {
            deprecated = true,
            message = "2FA is deprecated. Use API key authentication instead."
        });
    }

    /// <summary>
    /// DEPRECATED: Initial setup is no longer required.
    /// </summary>
    [HttpPost("setup")]
    public ActionResult Setup()
    {
        return Ok(new
        {
            deprecated = true,
            message = "Setup is deprecated. API key is generated automatically on first run.",
            instructions = new[]
            {
                "The server automatically generates an API key on first startup.",
                "Check the server console output for your API key.",
                "Or set the ADMIN_API_KEY environment variable before starting."
            }
        });
    }

    /// <summary>
    /// Get authentication status (for backward compatibility).
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        return Ok(new
        {
            authMethod = "api-key",
            deprecated = new[] { "login", "logout", "2fa", "sessions" },
            message = "Use X-API-Key header for authentication"
        });
    }
}
