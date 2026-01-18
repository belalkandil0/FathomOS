// LicensingSystem.Server/Controllers/SetupController.cs
// First-time admin setup API endpoints

using Microsoft.AspNetCore.Mvc;
using LicensingSystem.Server.Services;

namespace LicensingSystem.Server.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly ISetupService _setupService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        ISetupService setupService,
        IAuditService auditService,
        ILogger<SetupController> logger)
    {
        _setupService = setupService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get setup status - check if setup is required
    /// GET /api/setup/status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<SetupStatusResponse>> GetStatus()
    {
        try
        {
            var status = await _setupService.GetSetupStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setup status");
            return StatusCode(500, new { message = "Error checking setup status." });
        }
    }

    /// <summary>
    /// Validate a setup token
    /// POST /api/setup/validate-token
    /// </summary>
    [HttpPost("validate-token")]
    public async Task<ActionResult<ValidateTokenResponse>> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "Setup token is required." });
        }

        var clientIp = GetClientIp();

        try
        {
            var result = await _setupService.ValidateSetupTokenAsync(request.Token, clientIp);

            if (!result.IsValid)
            {
                return BadRequest(new ValidateTokenResponse
                {
                    Valid = false,
                    Message = result.ErrorMessage
                });
            }

            return Ok(new ValidateTokenResponse
            {
                Valid = true,
                Message = "Token validated successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating setup token");
            return StatusCode(500, new { message = "Error validating token." });
        }
    }

    /// <summary>
    /// Complete the initial setup by creating the first admin user
    /// POST /api/setup/complete
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<SetupCompleteResponse>> Complete([FromBody] SetupCompleteRequest request)
    {
        var clientIp = GetClientIp();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required." });
        }

        try
        {
            var result = await _setupService.CompleteSetupAsync(new SetupCompletionRequest
            {
                SetupToken = request.SetupToken,
                Email = request.Email,
                Username = request.Username,
                Password = request.Password,
                DisplayName = request.DisplayName,
                EnableTwoFactor = request.EnableTwoFactor
            }, clientIp);

            if (!result.Success)
            {
                return BadRequest(new SetupCompleteResponse
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }

            return Ok(new SetupCompleteResponse
            {
                Success = true,
                Message = result.Message,
                RedirectUrl = "/portal.html"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing setup");
            return StatusCode(500, new { message = "Error completing setup." });
        }
    }

    /// <summary>
    /// Get password requirements
    /// GET /api/setup/password-requirements
    /// </summary>
    [HttpGet("password-requirements")]
    public ActionResult<PasswordRequirementsResponse> GetPasswordRequirements()
    {
        return Ok(new PasswordRequirementsResponse
        {
            MinLength = 12,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialChar = true,
            SpecialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?"
        });
    }

    /// <summary>
    /// Complete setup from Desktop UI (localhost only, no token required)
    /// POST /api/setup/ui-complete
    /// This endpoint allows the Desktop UI Manager to complete setup without a token
    /// when running on the same machine as the server (localhost)
    /// </summary>
    [HttpPost("ui-complete")]
    public async Task<ActionResult<SetupCompleteResponse>> UiComplete(
        [FromBody] SetupCompleteRequest request)
    {
        // Get raw client IP without considering X-Forwarded-For (for security)
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Security: Only allow from localhost
        if (!IsLocalhost(clientIp))
        {
            await _auditService.LogAsync("SETUP_UI_BLOCKED", "Setup", null, null,
                request.Email, clientIp, "UI setup attempted from non-localhost address", false);

            _logger.LogWarning("Setup UI endpoint called from non-localhost: {ClientIp}", clientIp);

            return StatusCode(403, new
            {
                success = false,
                message = "This endpoint is only available from localhost connections."
            });
        }

        // Check if setup is still required
        var status = await _setupService.GetSetupStatusAsync();
        if (!status.SetupRequired)
        {
            return BadRequest(new SetupCompleteResponse
            {
                Success = false,
                Message = "Setup has already been completed."
            });
        }

        // Validate required fields (same as Complete endpoint)
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { success = false, message = "Email is required." });
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { success = false, message = "Username is required." });
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Password is required." });

        try
        {
            // Complete setup without token requirement (localhost bypass)
            var result = await _setupService.CompleteSetupAsync(new SetupCompletionRequest
            {
                SetupToken = null,  // No token required for localhost
                Email = request.Email,
                Username = request.Username,
                Password = request.Password,
                DisplayName = request.DisplayName ?? request.Username,
                EnableTwoFactor = request.EnableTwoFactor
            }, "localhost-ui");

            if (!result.Success)
            {
                return BadRequest(new SetupCompleteResponse
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }

            await _auditService.LogAsync("SETUP_UI_COMPLETED", "Setup",
                result.AdminUserId?.ToString(), null, request.Email, clientIp,
                "Setup completed via Desktop UI (localhost)", true);

            _logger.LogInformation("Setup completed via Desktop UI for admin: {Username}", request.Username);

            return Ok(new SetupCompleteResponse
            {
                Success = true,
                Message = "Setup completed successfully! You can now use the Desktop UI.",
                RedirectUrl = null  // Desktop UI handles its own navigation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing UI setup");
            return StatusCode(500, new { success = false, message = "Error completing setup." });
        }
    }

    private string GetClientIp()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Check if the given IP address is localhost
    /// </summary>
    private static bool IsLocalhost(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        // IPv4 localhost
        if (ipAddress == "127.0.0.1" || ipAddress.StartsWith("127."))
            return true;

        // IPv6 localhost
        if (ipAddress == "::1")
            return true;

        // IPv4-mapped IPv6 localhost
        if (ipAddress == "::ffff:127.0.0.1" || ipAddress.StartsWith("::ffff:127."))
            return true;

        return false;
    }
}

// ==================== Request/Response Models ====================

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ValidateTokenResponse
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
}

public class SetupCompleteRequest
{
    public string? SetupToken { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool EnableTwoFactor { get; set; }
}

public class SetupCompleteResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? RedirectUrl { get; set; }
}

public class PasswordRequirementsResponse
{
    public int MinLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireSpecialChar { get; set; }
    public string SpecialChars { get; set; } = string.Empty;
}
