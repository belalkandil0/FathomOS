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

    private string GetClientIp()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
