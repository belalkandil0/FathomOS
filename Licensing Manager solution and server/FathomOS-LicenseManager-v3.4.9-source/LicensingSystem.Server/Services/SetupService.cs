// LicensingSystem.Server/Services/SetupService.cs
// First-time admin setup service with token-based authentication

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LicensingSystem.Server.Services;

public interface ISetupService
{
    /// <summary>
    /// Check if first-time setup is required (no admin users exist)
    /// </summary>
    Task<bool> IsSetupRequiredAsync();

    /// <summary>
    /// Generate a new setup token (32 bytes random, 24 hour expiry)
    /// Returns the plain token (only shown once in console)
    /// </summary>
    Task<string> GenerateSetupTokenAsync();

    /// <summary>
    /// Validate a setup token against the stored hash
    /// </summary>
    Task<SetupTokenValidationResult> ValidateSetupTokenAsync(string token, string ipAddress);

    /// <summary>
    /// Complete the setup by creating the first admin user
    /// </summary>
    Task<SetupCompletionResult> CompleteSetupAsync(SetupCompletionRequest request, string ipAddress);

    /// <summary>
    /// Try to auto-setup from environment variables (ADMIN_EMAIL, ADMIN_USERNAME, ADMIN_PASSWORD)
    /// </summary>
    Task<bool> TryAutoSetupFromEnvironmentAsync();

    /// <summary>
    /// Get the current setup status
    /// </summary>
    Task<SetupStatusResponse> GetSetupStatusAsync();
}

public class SetupService : ISetupService
{
    private readonly LicenseDbContext _db;
    private readonly IAuditService _auditService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<SetupService> _logger;

    // Rate limiting for setup attempts
    private const int MaxSetupAttemptsPerWindow = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public SetupService(
        LicenseDbContext db,
        IAuditService auditService,
        IRateLimitService rateLimitService,
        ILogger<SetupService> logger)
    {
        _db = db;
        _auditService = auditService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task<bool> IsSetupRequiredAsync()
    {
        // Check if any admin users exist
        var hasAdminUsers = await _db.AdminUsers.AnyAsync();
        if (hasAdminUsers)
        {
            return false;
        }

        // Also check if setup was explicitly marked as completed
        var setupConfig = await _db.SetupConfigs.FirstOrDefaultAsync();
        return setupConfig == null || !setupConfig.IsSetupCompleted;
    }

    public async Task<string> GenerateSetupTokenAsync()
    {
        // Generate 32 bytes of random data
        var tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Hash the token for storage
        var tokenHash = HashToken(token);

        // Get or create setup config
        var config = await _db.SetupConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            config = new SetupConfigRecord();
            _db.SetupConfigs.Add(config);
        }

        config.SetupTokenHash = tokenHash;
        config.SetupTokenGeneratedAt = DateTime.UtcNow;
        config.SetupTokenExpiresAt = DateTime.UtcNow.AddHours(24);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Setup token generated, expires at {ExpiresAt}", config.SetupTokenExpiresAt);

        return token;
    }

    public async Task<SetupTokenValidationResult> ValidateSetupTokenAsync(string token, string ipAddress)
    {
        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(ipAddress, "setup-token", MaxSetupAttemptsPerWindow, RateLimitWindow))
        {
            await _auditService.LogAsync("SETUP_TOKEN_RATE_LIMITED", "Setup", null, null, null, ipAddress,
                "Too many setup token validation attempts", false);

            return new SetupTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Too many attempts. Please wait 15 minutes before trying again."
            };
        }

        var config = await _db.SetupConfigs.FirstOrDefaultAsync();
        if (config == null || string.IsNullOrEmpty(config.SetupTokenHash))
        {
            await _auditService.LogAsync("SETUP_TOKEN_INVALID", "Setup", null, null, null, ipAddress,
                "No setup token exists", false);

            return new SetupTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "No setup token exists. Please restart the server to generate a new token."
            };
        }

        // Check expiration
        if (config.SetupTokenExpiresAt.HasValue && config.SetupTokenExpiresAt.Value < DateTime.UtcNow)
        {
            await _auditService.LogAsync("SETUP_TOKEN_EXPIRED", "Setup", null, null, null, ipAddress,
                "Setup token has expired", false);

            return new SetupTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Setup token has expired. Please restart the server to generate a new token."
            };
        }

        // Validate token hash
        var providedHash = HashToken(token);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(config.SetupTokenHash),
            Encoding.UTF8.GetBytes(providedHash)))
        {
            await _auditService.LogAsync("SETUP_TOKEN_INVALID", "Setup", null, null, null, ipAddress,
                "Invalid setup token provided", false);

            return new SetupTokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid setup token."
            };
        }

        await _auditService.LogAsync("SETUP_TOKEN_VALIDATED", "Setup", null, null, null, ipAddress,
            "Setup token validated successfully", true);

        return new SetupTokenValidationResult
        {
            IsValid = true
        };
    }

    public async Task<SetupCompletionResult> CompleteSetupAsync(SetupCompletionRequest request, string ipAddress)
    {
        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(ipAddress, "setup-complete", MaxSetupAttemptsPerWindow, RateLimitWindow))
        {
            await _auditService.LogAsync("SETUP_COMPLETE_RATE_LIMITED", "Setup", null, null, request.Email, ipAddress,
                "Too many setup completion attempts", false);

            return new SetupCompletionResult
            {
                Success = false,
                ErrorMessage = "Too many attempts. Please wait 15 minutes before trying again."
            };
        }

        // Check if setup is still required
        if (!await IsSetupRequiredAsync())
        {
            return new SetupCompletionResult
            {
                Success = false,
                ErrorMessage = "Setup has already been completed."
            };
        }

        // Validate setup token (unless bypassed by environment setup, file setup, or localhost UI setup)
        if (!string.IsNullOrEmpty(request.SetupToken))
        {
            var tokenValidation = await ValidateSetupTokenAsync(request.SetupToken, ipAddress);
            if (!tokenValidation.IsValid)
            {
                return new SetupCompletionResult
                {
                    Success = false,
                    ErrorMessage = tokenValidation.ErrorMessage
                };
            }
        }
        else if (!IsLocalhostOrInternalSetup(ipAddress))
        {
            // Token is required for non-localhost/non-internal requests
            _logger.LogWarning("Setup attempted without token from non-localhost: {IpAddress}", ipAddress);
            return new SetupCompletionResult
            {
                Success = false,
                ErrorMessage = "Setup token is required for remote setup."
            };
        }
        // If token is null and it's localhost or internal setup source, allow setup without token
        else
        {
            _logger.LogInformation("Setup proceeding without token for localhost/internal source: {IpAddress}", ipAddress);
        }

        // Validate password strength
        var passwordValidation = ValidatePassword(request.Password);
        if (!passwordValidation.IsValid)
        {
            await _auditService.LogAsync("SETUP_PASSWORD_WEAK", "Setup", null, null, request.Email, ipAddress,
                passwordValidation.ErrorMessage, false);

            return new SetupCompletionResult
            {
                Success = false,
                ErrorMessage = passwordValidation.ErrorMessage
            };
        }

        // Validate email format
        if (!IsValidEmail(request.Email))
        {
            return new SetupCompletionResult
            {
                Success = false,
                ErrorMessage = "Invalid email address format."
            };
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        {
            return new SetupCompletionResult
            {
                Success = false,
                ErrorMessage = "Username must be at least 3 characters."
            };
        }

        // Create the admin user
        var (hash, salt) = HashPassword(request.Password);

        var admin = new AdminUserRecord
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            Username = request.Username.Trim(),
            DisplayName = request.DisplayName ?? request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "SuperAdmin",
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.AdminUsers.Add(admin);

        // Mark setup as completed
        var config = await _db.SetupConfigs.FirstOrDefaultAsync();
        if (config == null)
        {
            config = new SetupConfigRecord();
            _db.SetupConfigs.Add(config);
        }

        config.IsSetupCompleted = true;
        config.SetupCompletedAt = DateTime.UtcNow;
        config.SetupCompletedByIp = ipAddress;
        config.SetupMethod = DetermineSetupMethod(request.SetupToken, ipAddress);
        config.SetupTokenHash = null; // Clear the token after successful setup
        config.SetupTokenExpiresAt = null;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("SETUP_COMPLETED", "Setup", admin.Id.ToString(), null, admin.Email, ipAddress,
            $"Initial setup completed via {config.SetupMethod}", true);

        _logger.LogInformation("Initial setup completed. Admin user '{Username}' created.", admin.Username);

        return new SetupCompletionResult
        {
            Success = true,
            AdminUserId = admin.Id,
            Message = "Setup completed successfully! You can now log in with your credentials."
        };
    }

    public async Task<bool> TryAutoSetupFromEnvironmentAsync()
    {
        // Check if setup is required
        if (!await IsSetupRequiredAsync())
        {
            return false;
        }

        var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var username = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        _logger.LogInformation("Auto-setup from environment variables detected.");

        var result = await CompleteSetupAsync(new SetupCompletionRequest
        {
            Email = email,
            Username = username,
            Password = password,
            DisplayName = username
        }, "localhost");

        if (result.Success)
        {
            _logger.LogInformation("Auto-setup from environment variables completed successfully.");
            return true;
        }
        else
        {
            _logger.LogWarning("Auto-setup from environment variables failed: {Error}", result.ErrorMessage);
            return false;
        }
    }

    public async Task<SetupStatusResponse> GetSetupStatusAsync()
    {
        var setupRequired = await IsSetupRequiredAsync();
        var config = await _db.SetupConfigs.FirstOrDefaultAsync();

        return new SetupStatusResponse
        {
            SetupRequired = setupRequired,
            SetupCompleted = config?.IsSetupCompleted ?? false,
            SetupCompletedAt = config?.SetupCompletedAt,
            SetupMethod = config?.SetupMethod,
            TokenRequired = setupRequired && config?.SetupTokenHash != null,
            TokenExpired = config?.SetupTokenExpiresAt.HasValue == true && config.SetupTokenExpiresAt.Value < DateTime.UtcNow
        };
    }

    // ==================== Password Validation ====================

    private static PasswordValidationResult ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password is required." };
        }

        if (password.Length < 12)
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must be at least 12 characters long." };
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one uppercase letter." };
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one lowercase letter." };
        }

        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one digit." };
        }

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
        {
            return new PasswordValidationResult { IsValid = false, ErrorMessage = "Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;':\",./<>?)." };
        }

        return new PasswordValidationResult { IsValid = true };
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    // ==================== Hashing Utilities ====================

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static (string Hash, string Salt) HashPassword(string password)
    {
        var saltBytes = new byte[16];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        var hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return (hash, salt);
    }

    // ==================== Localhost/Internal Setup Helpers ====================

    /// <summary>
    /// Check if the IP address is localhost or an internal setup source
    /// </summary>
    private static bool IsLocalhostOrInternalSetup(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        // Internal setup sources (file-based, environment)
        if (ipAddress == "localhost" ||
            ipAddress == "file-based-setup" ||
            ipAddress == "localhost-ui" ||
            ipAddress == "environment")
            return true;

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

    /// <summary>
    /// Determine the setup method based on token and IP address
    /// </summary>
    private static string DetermineSetupMethod(string? setupToken, string ipAddress)
    {
        if (!string.IsNullOrEmpty(setupToken))
            return "Token";

        return ipAddress switch
        {
            "file-based-setup" => "File",
            "localhost-ui" => "DesktopUI",
            "localhost" or "127.0.0.1" or "::1" => "Environment",
            _ => "Unknown"
        };
    }
}

// ==================== Request/Response Models ====================

public class SetupTokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SetupCompletionRequest
{
    public string? SetupToken { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool EnableTwoFactor { get; set; }
}

public class SetupCompletionResult
{
    public bool Success { get; set; }
    public int? AdminUserId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SetupStatusResponse
{
    public bool SetupRequired { get; set; }
    public bool SetupCompleted { get; set; }
    public DateTime? SetupCompletedAt { get; set; }
    public string? SetupMethod { get; set; }
    public bool TokenRequired { get; set; }
    public bool TokenExpired { get; set; }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
