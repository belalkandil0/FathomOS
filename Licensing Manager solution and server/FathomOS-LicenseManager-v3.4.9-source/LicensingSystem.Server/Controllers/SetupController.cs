// LicensingSystem.Server/Controllers/SetupController.cs
// DEPRECATED - Setup is no longer required
// The server uses API key authentication now
// This controller returns deprecation messages for backward compatibility

using Microsoft.AspNetCore.Mvc;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// DEPRECATED: Setup is no longer required.
/// The server automatically generates an API key on first run.
/// </summary>
[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly ILogger<SetupController> _logger;

    public SetupController(ILogger<SetupController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get setup status - always returns setup complete.
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        return Ok(new
        {
            setupRequired = false,
            setupCompleted = true,
            setupMethod = "api-key",
            message = "Setup is no longer required. Use API key authentication.",
            instructions = new[]
            {
                "The server automatically generates an API key on first startup.",
                "Check the server console output for your API key.",
                "Or set the ADMIN_API_KEY environment variable before starting."
            }
        });
    }

    /// <summary>
    /// DEPRECATED: Token validation is no longer used.
    /// </summary>
    [HttpPost("validate-token")]
    public ActionResult ValidateToken()
    {
        return Ok(new
        {
            deprecated = true,
            valid = false,
            message = "Setup tokens are deprecated. Use API key authentication."
        });
    }

    /// <summary>
    /// DEPRECATED: Setup completion is no longer needed.
    /// </summary>
    [HttpPost("complete")]
    public ActionResult Complete()
    {
        return Ok(new
        {
            deprecated = true,
            success = false,
            message = "Setup is deprecated. API key is generated automatically on first run."
        });
    }

    /// <summary>
    /// DEPRECATED: UI setup is no longer needed.
    /// </summary>
    [HttpPost("ui-complete")]
    public ActionResult UiComplete()
    {
        return Ok(new
        {
            deprecated = true,
            success = false,
            message = "Setup is deprecated. API key is generated automatically on first run."
        });
    }

    /// <summary>
    /// Get password requirements (kept for reference).
    /// </summary>
    [HttpGet("password-requirements")]
    public ActionResult GetPasswordRequirements()
    {
        return Ok(new
        {
            deprecated = true,
            message = "Password-based authentication is deprecated. Use API key instead."
        });
    }
}
