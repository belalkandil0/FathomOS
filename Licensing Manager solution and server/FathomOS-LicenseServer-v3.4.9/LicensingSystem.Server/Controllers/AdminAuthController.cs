// LicensingSystem.Server/Controllers/AdminAuthController.cs
// Admin authentication with 2FA (TOTP) support

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;
using System.Security.Cryptography;
using System.Text;

namespace LicensingSystem.Server.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly IAuditService _auditService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(
        LicenseDbContext db,
        IAuditService auditService,
        IRateLimitService rateLimitService,
        ILogger<AdminAuthController> logger)
    {
        _db = db;
        _auditService = auditService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// Login to admin panel
    /// POST /api/admin/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var clientIp = GetClientIp();

        // Check if IP is blocked
        if (await _rateLimitService.IsIpBlockedAsync(clientIp))
        {
            return StatusCode(403, new { message = "Access denied. IP is blocked." });
        }

        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "admin-login", 5, TimeSpan.FromMinutes(15)))
        {
            await _auditService.LogAsync("LOGIN_RATE_LIMITED", "Admin", null, null, request.Username, clientIp,
                "Too many login attempts", false);
            return StatusCode(429, new { message = "Too many login attempts. Please try again later." });
        }

        // Find user
        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => (u.Username == request.Username || u.Email == request.Username) && u.IsActive);

        if (user == null)
        {
            await _auditService.LogAsync("LOGIN_FAILED", "Admin", null, null, request.Username, clientIp,
                "User not found", false);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        // Check if locked out
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            return StatusCode(423, new { message = $"Account locked. Try again in {remainingMinutes} minutes." });
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            user.FailedLoginAttempts++;
            
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                await _db.SaveChangesAsync();
                
                await _auditService.LogAsync("ACCOUNT_LOCKED", "Admin", user.Id.ToString(), null, user.Email, clientIp,
                    "Too many failed attempts", false);
                
                return StatusCode(423, new { message = "Account locked due to too many failed attempts." });
            }

            await _db.SaveChangesAsync();
            
            await _auditService.LogAsync("LOGIN_FAILED", "Admin", user.Id.ToString(), null, user.Email, clientIp,
                "Invalid password", false);
            
            return Unauthorized(new { message = "Invalid username or password." });
        }

        // Password correct - check if 2FA is required
        if (user.TwoFactorEnabled)
        {
            // Generate temporary token for 2FA step
            var tempToken = GenerateToken();
            
            // Store in session (valid for 5 minutes)
            var session = new AdminSessionRecord
            {
                AdminUserId = user.Id,
                SessionToken = tempToken,
                IpAddress = clientIp,
                UserAgent = Request.Headers["User-Agent"].FirstOrDefault(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                TwoFactorVerified = false
            };

            _db.AdminSessions.Add(session);
            await _db.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                RequiresTwoFactor = true,
                TempToken = tempToken,
                Message = "Enter your 2FA code to continue."
            });
        }

        // No 2FA - create full session
        return await CreateSession(user, clientIp);
    }

    /// <summary>
    /// Verify 2FA code
    /// POST /api/admin/auth/verify-2fa
    /// </summary>
    [HttpPost("verify-2fa")]
    public async Task<ActionResult<LoginResponse>> Verify2FA([FromBody] Verify2FARequest request)
    {
        var clientIp = GetClientIp();

        // Find temp session
        var session = await _db.AdminSessions
            .FirstOrDefaultAsync(s => s.SessionToken == request.TempToken && 
                                      !s.TwoFactorVerified && 
                                      s.ExpiresAt > DateTime.UtcNow);

        if (session == null)
        {
            return Unauthorized(new { message = "Session expired. Please login again." });
        }

        var user = await _db.AdminUsers.FindAsync(session.AdminUserId);
        if (user == null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        // Verify TOTP code
        if (!VerifyTotpCode(user.TwoFactorSecret!, request.Code))
        {
            await _auditService.LogAsync("2FA_FAILED", "Admin", user.Id.ToString(), null, user.Email, clientIp,
                "Invalid 2FA code", false);
            return Unauthorized(new { message = "Invalid verification code." });
        }

        // 2FA verified - upgrade session
        session.TwoFactorVerified = true;
        session.ExpiresAt = DateTime.UtcNow.AddHours(8);
        await _db.SaveChangesAsync();

        // Reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("LOGIN_SUCCESS", "Admin", user.Id.ToString(), null, user.Email, clientIp,
            "Login with 2FA", true);

        return Ok(new LoginResponse
        {
            Success = true,
            SessionToken = session.SessionToken,
            User = new AdminUserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role
            },
            ExpiresAt = session.ExpiresAt
        });
    }

    /// <summary>
    /// Setup 2FA for user
    /// POST /api/admin/auth/setup-2fa
    /// </summary>
    [HttpPost("setup-2fa")]
    public async Task<ActionResult<Setup2FAResponse>> Setup2FA([FromBody] Setup2FARequest request)
    {
        var session = await ValidateSessionAsync(request.SessionToken);
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var user = await _db.AdminUsers.FindAsync(session.AdminUserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (user.TwoFactorEnabled)
        {
            return BadRequest(new { message = "2FA is already enabled." });
        }

        // Generate secret
        var secret = GenerateTotpSecret();
        
        // Create otpauth URL for QR code
        var issuer = "FathomOS-License";
        var otpauthUrl = $"otpauth://totp/{issuer}:{user.Email}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

        // Store secret (not enabled yet until verified)
        user.TwoFactorSecret = secret;
        await _db.SaveChangesAsync();

        return Ok(new Setup2FAResponse
        {
            Secret = secret,
            QrCodeUrl = otpauthUrl,
            Message = "Scan the QR code with your authenticator app, then verify with a code."
        });
    }

    /// <summary>
    /// Confirm 2FA setup
    /// POST /api/admin/auth/confirm-2fa
    /// </summary>
    [HttpPost("confirm-2fa")]
    public async Task<ActionResult> Confirm2FA([FromBody] Confirm2FARequest request)
    {
        var session = await ValidateSessionAsync(request.SessionToken);
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var user = await _db.AdminUsers.FindAsync(session.AdminUserId);
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return BadRequest(new { message = "2FA setup not initiated." });
        }

        // Verify code
        if (!VerifyTotpCode(user.TwoFactorSecret, request.Code))
        {
            return BadRequest(new { message = "Invalid verification code. Please try again." });
        }

        // Generate backup codes
        var backupCodes = GenerateBackupCodes();
        user.BackupCodes = System.Text.Json.JsonSerializer.Serialize(backupCodes);
        user.TwoFactorEnabled = true;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("2FA_ENABLED", "Admin", user.Id.ToString(), null, user.Email, GetClientIp(),
            "2FA enabled", true);

        return Ok(new
        {
            message = "2FA enabled successfully!",
            backupCodes = backupCodes
        });
    }

    /// <summary>
    /// Disable 2FA
    /// POST /api/admin/auth/disable-2fa
    /// </summary>
    [HttpPost("disable-2fa")]
    public async Task<ActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        var session = await ValidateSessionAsync(request.SessionToken);
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var user = await _db.AdminUsers.FindAsync(session.AdminUserId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { message = "Invalid password." });
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.BackupCodes = null;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("2FA_DISABLED", "Admin", user.Id.ToString(), null, user.Email, GetClientIp(),
            "2FA disabled", true);

        return Ok(new { message = "2FA disabled." });
    }

    /// <summary>
    /// Logout
    /// POST /api/admin/auth/logout
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
    {
        var session = await _db.AdminSessions
            .FirstOrDefaultAsync(s => s.SessionToken == request.SessionToken);

        if (session != null)
        {
            session.IsValid = false;
            await _db.SaveChangesAsync();

            await _auditService.LogAsync("LOGOUT", "Admin", session.AdminUserId.ToString(), null, null, GetClientIp(),
                "User logged out", true);
        }

        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Get all active sessions for current user
    /// GET /api/admin/auth/sessions?sessionToken=xxx
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<SessionInfo>>> GetSessions([FromQuery] string sessionToken)
    {
        var session = await ValidateSessionAsync(sessionToken);
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var sessions = await _db.AdminSessions
            .Where(s => s.AdminUserId == session.AdminUserId && s.IsValid && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new SessionInfo
            {
                SessionId = s.Id,
                IpAddress = s.IpAddress,
                UserAgent = s.UserAgent,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                IsCurrent = s.SessionToken == sessionToken
            })
            .ToListAsync();

        return Ok(sessions);
    }

    /// <summary>
    /// Terminate a specific session (force logout)
    /// DELETE /api/admin/auth/sessions/{sessionId}
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult> TerminateSession(int sessionId, [FromQuery] string sessionToken)
    {
        var currentSession = await ValidateSessionAsync(sessionToken);
        if (currentSession == null)
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var targetSession = await _db.AdminSessions.FindAsync(sessionId);
        if (targetSession == null || targetSession.AdminUserId != currentSession.AdminUserId)
        {
            return NotFound(new { message = "Session not found." });
        }

        targetSession.IsValid = false;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("SESSION_TERMINATED", "Admin", targetSession.AdminUserId.ToString(), 
            null, null, GetClientIp(), $"Session {sessionId} terminated", true);

        return Ok(new { message = "Session terminated." });
    }

    /// <summary>
    /// Create initial admin user (only if no admins exist)
    /// POST /api/admin/auth/setup
    /// </summary>
    [HttpPost("setup")]
    public async Task<ActionResult> InitialSetup([FromBody] InitialSetupRequest request)
    {
        // Check if any admin exists
        if (await _db.AdminUsers.AnyAsync())
        {
            return BadRequest(new { message = "Admin setup already completed." });
        }

        var (hash, salt) = HashPassword(request.Password);

        var admin = new AdminUserRecord
        {
            Email = request.Email,
            Username = request.Username,
            DisplayName = request.DisplayName ?? request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "SuperAdmin",
            TwoFactorEnabled = false
        };

        _db.AdminUsers.Add(admin);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("ADMIN_CREATED", "Admin", admin.Id.ToString(), null, admin.Email, GetClientIp(),
            "Initial admin setup", true);

        return Ok(new { message = "Admin account created successfully." });
    }

    // ==================== Helper Methods ====================

    private async Task<LoginResponse> CreateSession(AdminUserRecord user, string clientIp)
    {
        var sessionToken = GenerateToken();
        
        var session = new AdminSessionRecord
        {
            AdminUserId = user.Id,
            SessionToken = sessionToken,
            IpAddress = clientIp,
            UserAgent = Request.Headers["User-Agent"].FirstOrDefault(),
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            TwoFactorVerified = !user.TwoFactorEnabled
        };

        _db.AdminSessions.Add(session);
        
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("LOGIN_SUCCESS", "Admin", user.Id.ToString(), null, user.Email, clientIp,
            "Login successful", true);

        return new LoginResponse
        {
            Success = true,
            SessionToken = sessionToken,
            User = new AdminUserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role
            },
            ExpiresAt = session.ExpiresAt
        };
    }

    private async Task<AdminSessionRecord?> ValidateSessionAsync(string? sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken)) return null;

        var session = await _db.AdminSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && 
                                      s.IsValid && 
                                      s.TwoFactorVerified &&
                                      s.ExpiresAt > DateTime.UtcNow);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return session;
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

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
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

    private static bool VerifyPassword(string password, string hash, string? salt)
    {
        if (string.IsNullOrEmpty(salt)) return false;

        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        var computedHash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        return hash == computedHash;
    }

    private static string GenerateTotpSecret()
    {
        var bytes = new byte[20];
        RandomNumberGenerator.Fill(bytes);
        return Base32Encode(bytes);
    }

    private static bool VerifyTotpCode(string secret, string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 6) return false;

        var secretBytes = Base32Decode(secret);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // Check current, previous, and next time window
        for (int i = -1; i <= 1; i++)
        {
            var totp = GenerateTotpCode(secretBytes, timestamp + i);
            if (totp == code) return true;
        }

        return false;
    }

    private static string GenerateTotpCode(byte[] secret, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) |
                     ((hash[offset + 1] & 0xFF) << 16) |
                     ((hash[offset + 2] & 0xFF) << 8) |
                     (hash[offset + 3] & 0xFF);

        var otp = binary % 1000000;
        return otp.ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();
        int buffer = 0, bitsInBuffer = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5)
            {
                var index = (buffer >> (bitsInBuffer - 5)) & 0x1F;
                result.Append(alphabet[index]);
                bitsInBuffer -= 5;
            }
        }

        if (bitsInBuffer > 0)
        {
            var index = (buffer << (5 - bitsInBuffer)) & 0x1F;
            result.Append(alphabet[index]);
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new List<byte>();
        int buffer = 0, bitsInBuffer = 0;

        foreach (var c in input.ToUpperInvariant())
        {
            var index = alphabet.IndexOf(c);
            if (index < 0) continue;

            buffer = (buffer << 5) | index;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                result.Add((byte)(buffer >> (bitsInBuffer - 8)));
                bitsInBuffer -= 8;
            }
        }

        return result.ToArray();
    }

    private static List<string> GenerateBackupCodes()
    {
        var codes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var code = BitConverter.ToUInt32(bytes, 0) % 100000000;
            codes.Add(code.ToString("D8"));
        }
        return codes;
    }
}

// ==================== Request/Response Models ====================

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? TempToken { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
    public AdminUserInfo? User { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AdminUserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
}

public class Verify2FARequest
{
    public string TempToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class Setup2FARequest
{
    public string SessionToken { get; set; } = string.Empty;
}

public class Setup2FAResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class Confirm2FARequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class Disable2FARequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string SessionToken { get; set; } = string.Empty;
}

public class SessionInfo
{
    public int SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsCurrent { get; set; }
}

public class InitialSetupRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}
