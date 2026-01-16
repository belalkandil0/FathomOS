// LicensingSystem.Server/Controllers/CustomerPortalController.cs
// Customer-facing portal for license management (transfer, deactivation, status)

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;
using LicensingSystem.Shared;
using System.Security.Cryptography;
using System.Text.Json;

namespace LicensingSystem.Server.Controllers;

/// <summary>
/// Customer-facing portal for license management
/// Customers can transfer licenses, view status, and deactivate devices
/// </summary>
[ApiController]
[Route("api/portal")]
public class CustomerPortalController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly IAuditService _auditService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<CustomerPortalController> _logger;

    public CustomerPortalController(
        LicenseDbContext db,
        IAuditService auditService,
        IRateLimitService rateLimitService,
        ILogger<CustomerPortalController> logger)
    {
        _db = db;
        _auditService = auditService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// Verify license credentials and return license status
    /// POST /api/portal/verify
    /// </summary>
    [HttpPost("verify")]
    public async Task<ActionResult<PortalLicenseStatus>> VerifyLicense([FromBody] PortalVerifyRequest request)
    {
        var clientIp = GetClientIp();
        
        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "portal-verify", 10, TimeSpan.FromMinutes(5)))
        {
            await _auditService.LogAsync("RATE_LIMIT", "Portal", null, null, request.Email, clientIp,
                "Portal verify rate limit exceeded", false);
            return StatusCode(429, new { message = "Too many attempts. Please try again later." });
        }

        // Find license by key
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Key == request.LicenseKey);

        if (license == null)
        {
            await _auditService.LogAsync("PORTAL_VERIFY_FAILED", "License", null, null, request.Email, clientIp,
                "Invalid license key", false);
            return NotFound(new { message = "License not found. Please check your license key." });
        }

        // Verify email matches
        if (!license.CustomerEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.LogAsync("PORTAL_VERIFY_FAILED", "License", license.LicenseId, null, request.Email, clientIp,
                "Email mismatch", false);
            return Unauthorized(new { message = "Email does not match license records." });
        }

        // Verify support code if provided
        if (!string.IsNullOrEmpty(request.SupportCode) && 
            !string.IsNullOrEmpty(license.SupportCode) &&
            !license.SupportCode.Equals(request.SupportCode, StringComparison.OrdinalIgnoreCase))
        {
            await _auditService.LogAsync("PORTAL_VERIFY_FAILED", "License", license.LicenseId, null, request.Email, clientIp,
                "Support code mismatch", false);
            return Unauthorized(new { message = "Invalid support code." });
        }

        // Get active activation
        var activeActivation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);
        
        // Parse hardware fingerprints for display
        var hardwareInfo = ParseHardwareFingerprint(activeActivation?.HardwareFingerprint);

        await _auditService.LogAsync("PORTAL_VERIFY_SUCCESS", "License", license.LicenseId, null, request.Email, clientIp,
            "License verified successfully", true);

        return Ok(new PortalLicenseStatus
        {
            LicenseId = license.LicenseId,
            CustomerName = license.CustomerName,
            CustomerEmail = license.CustomerEmail,
            Edition = license.Edition,
            Brand = license.Brand,
            LicenseeCode = license.LicenseeCode,
            SupportCode = license.SupportCode,
            ExpiresAt = license.ExpiresAt,
            IsExpired = license.ExpiresAt < DateTime.UtcNow,
            IsRevoked = license.IsRevoked,
            LicenseType = license.LicenseType,
            SubscriptionType = license.SubscriptionType.ToString(),
            DaysRemaining = Math.Max(0, (int)(license.ExpiresAt - DateTime.UtcNow).TotalDays),
            
            // Active device info
            HasActiveDevice = activeActivation != null,
            ActiveDeviceName = activeActivation?.MachineName,
            ActiveDeviceHardware = hardwareInfo,
            LastSeenAt = activeActivation?.LastSeenAt,
            ActivatedAt = activeActivation?.ActivatedAt,
            
            // Transfer info
            CanTransfer = activeActivation != null && !license.IsRevoked && license.ExpiresAt > DateTime.UtcNow,
            TransferCount = await _db.LicenseTransfers.CountAsync(t => t.LicenseId == license.LicenseId && t.Status == "Completed"),
            
            // Session token for subsequent calls
            SessionToken = GeneratePortalSessionToken(license.LicenseId, request.Email)
        });
    }

    /// <summary>
    /// Request license transfer - sends verification email
    /// POST /api/portal/transfer/request
    /// </summary>
    [HttpPost("transfer/request")]
    public async Task<ActionResult> RequestTransfer([FromBody] PortalTransferRequest request)
    {
        var clientIp = GetClientIp();
        
        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "portal-transfer", 3, TimeSpan.FromHours(1)))
        {
            return StatusCode(429, new { message = "Too many transfer requests. Please try again later." });
        }

        // Validate session token
        if (!ValidatePortalSessionToken(request.SessionToken, request.LicenseId, request.Email))
        {
            return Unauthorized(new { message = "Invalid session. Please verify your license again." });
        }

        // Find license
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);

        if (license == null)
        {
            return NotFound(new { message = "License not found." });
        }

        // Check if transfer is allowed
        if (license.IsRevoked)
        {
            return BadRequest(new { message = "Cannot transfer a revoked license." });
        }

        if (license.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Cannot transfer an expired license." });
        }

        var activeActivation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);
        if (activeActivation == null)
        {
            return BadRequest(new { message = "No active device to transfer from." });
        }

        // Validate new hardware ID
        if (string.IsNullOrWhiteSpace(request.NewHardwareId))
        {
            return BadRequest(new { message = "New Hardware ID is required." });
        }

        // Check for pending transfers
        var pendingTransfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.LicenseId == request.LicenseId && 
                                      t.Status == "Pending" && 
                                      t.RequestedAt > DateTime.UtcNow.AddHours(-24));

        if (pendingTransfer != null)
        {
            return BadRequest(new { message = "A transfer is already pending. Check your email for the verification code." });
        }

        // Generate verification code
        var verificationCode = GenerateVerificationCode();
        var transferToken = GenerateTransferToken();

        // Create transfer record
        var transfer = new LicenseTransferRecord
        {
            LicenseId = license.LicenseId,
            TransferToken = transferToken,
            OldHardwareFingerprint = activeActivation.HardwareFingerprint,
            NewHardwareFingerprint = request.NewHardwareId,
            OldMachineName = activeActivation.MachineName,
            NewMachineName = request.NewMachineName,
            CustomerEmail = license.CustomerEmail,
            IpAddress = clientIp,
            Reason = request.Reason,
            Status = "Pending"
        };

        // Create verification record
        var verification = new TransferVerificationRecord
        {
            VerificationCode = verificationCode,
            Email = license.CustomerEmail,
            LicenseId = license.LicenseId,
            Purpose = "Transfer",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _db.LicenseTransfers.Add(transfer);
        _db.TransferVerifications.Add(verification);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("TRANSFER_REQUESTED", "License", license.LicenseId, null, license.CustomerEmail, clientIp,
            $"Transfer requested from {activeActivation.MachineName} to new device", true);

        _logger.LogInformation("Transfer requested for license {LicenseId}. Verification code: {Code}", 
            license.LicenseId, verificationCode);

        // TODO: Send email with verification code
        // For now, return the code in the response (in production, send via email only!)
        return Ok(new PortalTransferResponse
        {
            Message = $"Verification code sent to {MaskEmail(license.CustomerEmail)}",
            TransferToken = transferToken,
            ExpiresAt = verification.ExpiresAt,
            // TEMPORARY: Include code for testing - remove in production!
            DebugVerificationCode = verificationCode
        });
    }

    /// <summary>
    /// Complete license transfer with verification code
    /// POST /api/portal/transfer/complete
    /// </summary>
    [HttpPost("transfer/complete")]
    public async Task<ActionResult> CompleteTransfer([FromBody] PortalTransferCompleteRequest request)
    {
        var clientIp = GetClientIp();

        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "portal-transfer-verify", 5, TimeSpan.FromMinutes(15)))
        {
            return StatusCode(429, new { message = "Too many verification attempts." });
        }

        // Find transfer record
        var transfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.TransferToken == request.TransferToken && t.Status == "Pending");

        if (transfer == null)
        {
            return NotFound(new { message = "Transfer request not found or already completed." });
        }

        // Check if expired
        if (transfer.RequestedAt < DateTime.UtcNow.AddHours(-24))
        {
            transfer.Status = "Expired";
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Transfer request has expired. Please start a new transfer." });
        }

        // Verify the code
        var verification = await _db.TransferVerifications
            .FirstOrDefaultAsync(v => v.LicenseId == transfer.LicenseId && 
                                      v.Purpose == "Transfer" && 
                                      !v.IsUsed && 
                                      v.ExpiresAt > DateTime.UtcNow);

        if (verification == null)
        {
            return BadRequest(new { message = "Verification code expired. Please request a new transfer." });
        }

        if (verification.VerificationCode != request.VerificationCode)
        {
            verification.FailedAttempts++;
            
            if (verification.FailedAttempts >= 5)
            {
                verification.IsUsed = true;
                transfer.Status = "Cancelled";
                await _db.SaveChangesAsync();
                
                await _auditService.LogAsync("TRANSFER_CANCELLED", "License", transfer.LicenseId, null, transfer.CustomerEmail, clientIp,
                    "Too many failed verification attempts", false);
                
                return BadRequest(new { message = "Too many failed attempts. Transfer cancelled." });
            }

            await _db.SaveChangesAsync();
            return BadRequest(new { message = $"Invalid verification code. {5 - verification.FailedAttempts} attempts remaining." });
        }

        // Verification successful - perform the transfer
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseId == transfer.LicenseId);

        if (license == null)
        {
            return NotFound(new { message = "License not found." });
        }

        // Deactivate old device
        var oldActivation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);
        if (oldActivation != null)
        {
            oldActivation.IsDeactivated = true;
            oldActivation.DeactivatedAt = DateTime.UtcNow;
        }

        // Mark verification as used
        verification.IsUsed = true;

        // Update transfer record
        transfer.Status = "Completed";
        transfer.CompletedAt = DateTime.UtcNow;
        transfer.IsEmailVerified = true;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("TRANSFER_COMPLETED", "License", license.LicenseId, null, license.CustomerEmail, clientIp,
            $"License transferred from {transfer.OldMachineName} to new device. Old hardware deactivated.", true);

        _logger.LogInformation("License {LicenseId} transferred successfully", license.LicenseId);

        return Ok(new
        {
            Message = "License transferred successfully! You can now activate on your new device.",
            LicenseKey = license.Key,
            NewHardwareId = transfer.NewHardwareFingerprint
        });
    }

    /// <summary>
    /// Deactivate current device (customer-initiated)
    /// POST /api/portal/deactivate
    /// </summary>
    [HttpPost("deactivate")]
    public async Task<ActionResult> DeactivateDevice([FromBody] PortalDeactivateRequest request)
    {
        var clientIp = GetClientIp();

        // Rate limiting
        if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "portal-deactivate", 3, TimeSpan.FromHours(1)))
        {
            return StatusCode(429, new { message = "Too many deactivation requests." });
        }

        // Validate session token
        if (!ValidatePortalSessionToken(request.SessionToken, request.LicenseId, request.Email))
        {
            return Unauthorized(new { message = "Invalid session. Please verify your license again." });
        }

        // Find license and active activation
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);

        if (license == null)
        {
            return NotFound(new { message = "License not found." });
        }

        var activeActivation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);
        if (activeActivation == null)
        {
            return BadRequest(new { message = "No active device to deactivate." });
        }

        // Deactivate
        activeActivation.IsDeactivated = true;
        activeActivation.DeactivatedAt = DateTime.UtcNow;

        // Log the deactivation as a transfer
        var deactivationLog = new LicenseTransferRecord
        {
            LicenseId = license.LicenseId,
            TransferToken = GenerateTransferToken(),
            OldHardwareFingerprint = activeActivation.HardwareFingerprint,
            OldMachineName = activeActivation.MachineName,
            CustomerEmail = license.CustomerEmail,
            IpAddress = clientIp,
            Reason = request.Reason ?? "Customer requested deactivation",
            Status = "Completed",
            CompletedAt = DateTime.UtcNow,
            IsEmailVerified = true
        };

        _db.LicenseTransfers.Add(deactivationLog);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("DEVICE_DEACTIVATED", "License", license.LicenseId, null, license.CustomerEmail, clientIp,
            $"Device {activeActivation.MachineName} deactivated by customer", true);

        return Ok(new
        {
            Message = "Device deactivated successfully. You can now activate on a new device.",
            LicenseKey = license.Key
        });
    }

    /// <summary>
    /// Get transfer history for a license
    /// GET /api/portal/transfers?licenseId=xxx&sessionToken=xxx
    /// </summary>
    [HttpGet("transfers")]
    public async Task<ActionResult<List<TransferHistoryItem>>> GetTransferHistory(
        [FromQuery] string licenseId,
        [FromQuery] string sessionToken,
        [FromQuery] string email)
    {
        if (!ValidatePortalSessionToken(sessionToken, licenseId, email))
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var transfers = await _db.LicenseTransfers
            .Where(t => t.LicenseId == licenseId)
            .OrderByDescending(t => t.RequestedAt)
            .Take(20)
            .Select(t => new TransferHistoryItem
            {
                RequestedAt = t.RequestedAt,
                CompletedAt = t.CompletedAt,
                Status = t.Status,
                OldMachineName = t.OldMachineName,
                NewMachineName = t.NewMachineName,
                Reason = t.Reason
            })
            .ToListAsync();

        return Ok(transfers);
    }

    /// <summary>
    /// View hardware fingerprint details (help customer understand what's tracked)
    /// GET /api/portal/hardware-info?sessionToken=xxx&licenseId=xxx
    /// </summary>
    [HttpGet("hardware-info")]
    public async Task<ActionResult<HardwareInfoResponse>> GetHardwareInfo(
        [FromQuery] string licenseId,
        [FromQuery] string sessionToken,
        [FromQuery] string email)
    {
        if (!ValidatePortalSessionToken(sessionToken, licenseId, email))
        {
            return Unauthorized(new { message = "Invalid session." });
        }

        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license == null)
        {
            return NotFound(new { message = "License not found." });
        }

        var activeActivation = license.Activations.FirstOrDefault(a => !a.IsDeactivated);
        var hardwareInfo = ParseHardwareFingerprint(activeActivation?.HardwareFingerprint);

        return Ok(new HardwareInfoResponse
        {
            MachineName = activeActivation?.MachineName,
            HardwareComponents = hardwareInfo,
            LastSeenAt = activeActivation?.LastSeenAt,
            ActivatedAt = activeActivation?.ActivatedAt,
            IpAddress = MaskIpAddress(activeActivation?.IpAddress),
            AppVersion = activeActivation?.AppVersion,
            OsVersion = activeActivation?.OsVersion,
            Explanation = new HardwareExplanation
            {
                WhatIsTracked = "Your license is bound to specific hardware components in your computer to prevent unauthorized sharing.",
                ComponentsTracked = new List<string>
                {
                    "CPU ID - Your processor's unique identifier",
                    "Motherboard Serial - Your computer's mainboard identifier",
                    "MAC Address - Your network adapter's hardware address",
                    "Disk Serial - Your primary storage drive identifier"
                },
                WhyItMatters = "If you change significant hardware (like replacing the motherboard), you may need to transfer your license.",
                HowToTransfer = "Use the 'Transfer to New Device' option above to move your license to new hardware."
            }
        });
    }

    // ==================== Helper Methods ====================

    private string GetClientIp()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string GenerateVerificationCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
        return code.ToString("D6");
    }

    private static string GenerateTransferToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string GeneratePortalSessionToken(string licenseId, string email)
    {
        var data = $"{licenseId}:{email}:{DateTime.UtcNow:yyyyMMddHH}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static bool ValidatePortalSessionToken(string? token, string? licenseId, string? email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(licenseId) || string.IsNullOrEmpty(email))
            return false;

        // Token is valid for current hour and previous hour
        var currentToken = GeneratePortalSessionToken(licenseId, email);
        
        var previousData = $"{licenseId}:{email}:{DateTime.UtcNow.AddHours(-1):yyyyMMddHH}";
        var previousBytes = System.Text.Encoding.UTF8.GetBytes(previousData);
        var previousHash = SHA256.HashData(previousBytes);
        var previousToken = Convert.ToBase64String(previousHash).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        return token == currentToken || token == previousToken;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "***";
        
        var atIndex = email.IndexOf('@');
        if (atIndex <= 2) return email[0] + "***" + email[atIndex..];
        
        return email[..2] + "***" + email[(atIndex - 1)..];
    }

    private static string? MaskIpAddress(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return null;
        
        var parts = ip.Split('.');
        if (parts.Length == 4)
        {
            return $"{parts[0]}.{parts[1]}.***.***";
        }
        return ip[..Math.Min(ip.Length, 8)] + "***";
    }

    private static HardwareComponentInfo ParseHardwareFingerprint(string? fingerprint)
    {
        var info = new HardwareComponentInfo();
        
        if (string.IsNullOrEmpty(fingerprint))
            return info;

        var parts = fingerprint.Split('-');
        
        for (int i = 0; i < parts.Length && i < 4; i++)
        {
            var masked = MaskHardwareId(parts[i]);
            switch (i)
            {
                case 0: info.CpuId = masked; break;
                case 1: info.MotherboardId = masked; break;
                case 2: info.MacAddress = masked; break;
                case 3: info.DiskSerial = masked; break;
            }
        }

        return info;
    }

    private static string MaskHardwareId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length <= 4) return "****";
        return id[..4] + new string('*', id.Length - 4);
    }
}

// ==================== Request/Response Models ====================

public class PortalVerifyRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SupportCode { get; set; }
}

public class PortalLicenseStatus
{
    public string LicenseId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Edition { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsRevoked { get; set; }
    public string? LicenseType { get; set; }
    public string? SubscriptionType { get; set; }
    public int DaysRemaining { get; set; }
    
    public bool HasActiveDevice { get; set; }
    public string? ActiveDeviceName { get; set; }
    public HardwareComponentInfo? ActiveDeviceHardware { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    
    public bool CanTransfer { get; set; }
    public int TransferCount { get; set; }
    
    public string? SessionToken { get; set; }
}

public class HardwareComponentInfo
{
    public string? CpuId { get; set; }
    public string? MotherboardId { get; set; }
    public string? MacAddress { get; set; }
    public string? DiskSerial { get; set; }
}

public class PortalTransferRequest
{
    public string LicenseId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string NewHardwareId { get; set; } = string.Empty;
    public string? NewMachineName { get; set; }
    public string? Reason { get; set; }
}

public class PortalTransferResponse
{
    public string Message { get; set; } = string.Empty;
    public string TransferToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? DebugVerificationCode { get; set; } // Remove in production!
}

public class PortalTransferCompleteRequest
{
    public string TransferToken { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}

public class PortalDeactivateRequest
{
    public string LicenseId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class TransferHistoryItem
{
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OldMachineName { get; set; }
    public string? NewMachineName { get; set; }
    public string? Reason { get; set; }
}

public class HardwareInfoResponse
{
    public string? MachineName { get; set; }
    public HardwareComponentInfo? HardwareComponents { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
    public HardwareExplanation? Explanation { get; set; }
}

public class HardwareExplanation
{
    public string? WhatIsTracked { get; set; }
    public List<string>? ComponentsTracked { get; set; }
    public string? WhyItMatters { get; set; }
    public string? HowToTransfer { get; set; }
}
