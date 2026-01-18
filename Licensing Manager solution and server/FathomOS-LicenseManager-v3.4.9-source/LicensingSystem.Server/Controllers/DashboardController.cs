// LicensingSystem.Server/Controllers/DashboardController.cs
// Dashboard analytics, health monitoring, and backup management

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;

namespace LicensingSystem.Server.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly LicenseDbContext _db;
    private readonly IHealthMonitorService _healthMonitor;
    private readonly IBackupService _backupService;
    private readonly IAuditService _auditService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        LicenseDbContext db,
        IHealthMonitorService healthMonitor,
        IBackupService backupService,
        IAuditService auditService,
        IRateLimitService rateLimitService,
        ILogger<DashboardController> logger)
    {
        _db = db;
        _healthMonitor = healthMonitor;
        _backupService = backupService;
        _auditService = auditService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview statistics
    /// GET /api/dashboard/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardOverviewStats>> GetStats()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sevenDaysAgo = now.AddDays(-7);

        // License counts
        var totalLicenses = await _db.LicenseKeys.CountAsync();
        var activeLicenses = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt > now);
        var expiredLicenses = await _db.LicenseKeys.CountAsync(l => !l.IsRevoked && l.ExpiresAt <= now);
        var revokedLicenses = await _db.LicenseKeys.CountAsync(l => l.IsRevoked);

        // Expiring soon
        var expiringThisWeek = await _db.LicenseKeys
            .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= sevenDaysAgo.AddDays(14));
        var expiringThisMonth = await _db.LicenseKeys
            .CountAsync(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= now.AddDays(30));

        // By tier
        var byTier = await _db.LicenseKeys
            .Where(l => !l.IsRevoked && l.ExpiresAt > now)
            .GroupBy(l => l.Edition)
            .Select(g => new TierCount { Tier = g.Key, Count = g.Count() })
            .ToListAsync();

        // By license type
        var onlineLicenses = await _db.LicenseKeys.CountAsync(l => l.LicenseType == "Online" && !l.IsRevoked);
        var offlineLicenses = await _db.LicenseKeys.CountAsync(l => l.LicenseType == "Offline" && !l.IsRevoked);

        // Activations
        var totalActivations = await _db.LicenseActivations.CountAsync();
        var activeActivations = await _db.LicenseActivations.CountAsync(a => !a.IsDeactivated);
        var activationsThisMonth = await _db.LicenseActivations.CountAsync(a => a.ActivatedAt >= thirtyDaysAgo);
        var activationsThisWeek = await _db.LicenseActivations.CountAsync(a => a.ActivatedAt >= sevenDaysAgo);

        // Transfers
        var totalTransfers = await _db.LicenseTransfers.CountAsync(t => t.Status == "Completed");
        var transfersThisMonth = await _db.LicenseTransfers
            .CountAsync(t => t.Status == "Completed" && t.CompletedAt >= thirtyDaysAgo);

        // Active sessions
        var activeSessions = await _db.ActiveSessions.CountAsync(s => s.IsActive);

        return Ok(new DashboardOverviewStats
        {
            Licenses = new LicenseStatistics
            {
                Total = totalLicenses,
                Active = activeLicenses,
                Expired = expiredLicenses,
                Revoked = revokedLicenses,
                ExpiringThisWeek = expiringThisWeek,
                ExpiringThisMonth = expiringThisMonth,
                Online = onlineLicenses,
                Offline = offlineLicenses,
                ByTier = byTier
            },
            Activations = new ActivationStatistics
            {
                Total = totalActivations,
                Active = activeActivations,
                ThisMonth = activationsThisMonth,
                ThisWeek = activationsThisWeek
            },
            Transfers = new TransferStatistics
            {
                Total = totalTransfers,
                ThisMonth = transfersThisMonth
            },
            ActiveSessions = activeSessions,
            GeneratedAt = now
        });
    }

    /// <summary>
    /// Get license trend data for charts
    /// GET /api/dashboard/trends?days=30
    /// </summary>
    [HttpGet("trends")]
    public async Task<ActionResult<TrendData>> GetTrends([FromQuery] int days = 30)
    {
        var now = DateTime.UtcNow.Date;
        var startDate = now.AddDays(-days);

        // Activations per day
        var activationsPerDay = await _db.LicenseActivations
            .Where(a => a.ActivatedAt >= startDate)
            .GroupBy(a => a.ActivatedAt.Date)
            .Select(g => new DailyCount { Date = g.Key, Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Licenses created per day
        var licensesPerDay = await _db.LicenseKeys
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new DailyCount { Date = g.Key, Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Fill in missing dates
        var allDates = Enumerable.Range(0, days + 1)
            .Select(i => startDate.AddDays(i))
            .ToList();

        var activationTrend = allDates.Select(d => new DailyCount
        {
            Date = d,
            Count = activationsPerDay.FirstOrDefault(a => a.Date == d)?.Count ?? 0
        }).ToList();

        var licenseTrend = allDates.Select(d => new DailyCount
        {
            Date = d,
            Count = licensesPerDay.FirstOrDefault(l => l.Date == d)?.Count ?? 0
        }).ToList();

        // Expiring licenses timeline (next 30 days)
        var expiringTimeline = await _db.LicenseKeys
            .Where(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= now.AddDays(30))
            .GroupBy(l => l.ExpiresAt.Date)
            .Select(g => new DailyCount { Date = g.Key, Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return Ok(new TrendData
        {
            ActivationsPerDay = activationTrend,
            LicensesCreatedPerDay = licenseTrend,
            ExpiringTimeline = expiringTimeline
        });
    }

    /// <summary>
    /// Get expiring licenses list
    /// GET /api/dashboard/expiring?days=30
    /// </summary>
    [HttpGet("expiring")]
    public async Task<ActionResult<List<ExpiringLicense>>> GetExpiringLicenses([FromQuery] int days = 30)
    {
        var now = DateTime.UtcNow;
        var endDate = now.AddDays(days);

        var expiring = await _db.LicenseKeys
            .Where(l => !l.IsRevoked && l.ExpiresAt > now && l.ExpiresAt <= endDate)
            .OrderBy(l => l.ExpiresAt)
            .Select(l => new ExpiringLicense
            {
                LicenseId = l.LicenseId,
                Key = l.Key,
                CustomerName = l.CustomerName,
                CustomerEmail = l.CustomerEmail,
                Edition = l.Edition,
                ExpiresAt = l.ExpiresAt,
                DaysRemaining = (int)(l.ExpiresAt - now).TotalDays
            })
            .Take(100)
            .ToListAsync();

        return Ok(expiring);
    }

    /// <summary>
    /// Get health dashboard
    /// GET /api/dashboard/health
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<HealthDashboard>> GetHealthDashboard()
    {
        return Ok(await _healthMonitor.GetDashboardAsync());
    }

    /// <summary>
    /// Get recent audit logs
    /// GET /api/dashboard/audit?page=1&pageSize=50
    /// </summary>
    [HttpGet("audit")]
    public async Task<ActionResult<List<AuditLogRecord>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null)
    {
        return Ok(await _auditService.GetLogsAsync(page, pageSize, action, entityType));
    }

    /// <summary>
    /// Get available audit actions for filtering
    /// GET /api/dashboard/audit/actions
    /// </summary>
    [HttpGet("audit/actions")]
    public async Task<ActionResult<List<string>>> GetAuditActions()
    {
        var actions = await _db.AuditLogs
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return Ok(actions);
    }

    // ==================== Backup Endpoints ====================

    /// <summary>
    /// Create database backup
    /// POST /api/dashboard/backups
    /// </summary>
    [HttpPost("backups")]
    public async Task<ActionResult<DatabaseBackupRecord>> CreateBackup([FromBody] CreateBackupRequest request)
    {
        var backup = await _backupService.CreateBackupAsync(
            request.CreatedBy,
            request.Notes,
            request.BackupType ?? "Manual",
            request.Encrypt ?? true
        );

        return Ok(backup);
    }

    /// <summary>
    /// Get list of backups
    /// GET /api/dashboard/backups
    /// </summary>
    [HttpGet("backups")]
    public async Task<ActionResult<List<DatabaseBackupRecord>>> GetBackups([FromQuery] int limit = 50)
    {
        return Ok(await _backupService.GetBackupsAsync(limit));
    }

    /// <summary>
    /// Restore from backup
    /// POST /api/dashboard/backups/{id}/restore
    /// </summary>
    [HttpPost("backups/{id}/restore")]
    public async Task<ActionResult> RestoreBackup(int id, [FromBody] RestoreBackupRequest request)
    {
        var success = await _backupService.RestoreBackupAsync(id, request.DecryptionKey);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to restore backup." });
        }

        return Ok(new { message = "Backup restored successfully." });
    }

    /// <summary>
    /// Verify backup integrity
    /// POST /api/dashboard/backups/{id}/verify
    /// </summary>
    [HttpPost("backups/{id}/verify")]
    public async Task<ActionResult> VerifyBackup(int id)
    {
        var isValid = await _backupService.VerifyBackupAsync(id);
        
        return Ok(new { 
            valid = isValid,
            message = isValid ? "Backup is valid." : "Backup verification failed."
        });
    }

    /// <summary>
    /// Delete backup
    /// DELETE /api/dashboard/backups/{id}
    /// </summary>
    [HttpDelete("backups/{id}")]
    public async Task<ActionResult> DeleteBackup(int id)
    {
        var success = await _backupService.DeleteBackupAsync(id);
        
        if (!success)
        {
            return NotFound(new { message = "Backup not found." });
        }

        return Ok(new { message = "Backup deleted." });
    }

    /// <summary>
    /// Download backup file
    /// GET /api/dashboard/backups/{id}/download
    /// </summary>
    [HttpGet("backups/{id}/download")]
    public async Task<ActionResult> DownloadBackup(int id)
    {
        var backup = await _db.DatabaseBackups.FindAsync(id);
        if (backup == null)
        {
            return NotFound(new { message = "Backup not found." });
        }

        var stream = await _backupService.DownloadBackupAsync(id);
        return File(stream, "application/gzip", backup.FileName);
    }

    // ==================== Session Management Endpoints ====================

    /// <summary>
    /// Get active license sessions
    /// GET /api/dashboard/sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<ActiveSessionRecord>>> GetActiveSessions()
    {
        var sessions = await _db.ActiveSessions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastHeartbeat)
            .ToListAsync();

        return Ok(sessions);
    }

    /// <summary>
    /// Terminate a session
    /// POST /api/dashboard/sessions/{licenseId}/terminate
    /// </summary>
    [HttpPost("sessions/{licenseId}/terminate")]
    public async Task<ActionResult> TerminateSession(string licenseId, [FromBody] TerminateSessionRequest request)
    {
        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.LicenseId == licenseId && s.IsActive);

        if (session == null)
        {
            return NotFound(new { message = "No active session found for this license." });
        }

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        session.EndReason = request.Reason ?? "Terminated by admin";
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("SESSION_TERMINATED_BY_ADMIN", "Session", licenseId,
            null, null, GetClientIp(), $"Reason: {request.Reason}", true);

        return Ok(new { message = "Session terminated" });
    }

    // ==================== Security Endpoints ====================

    /// <summary>
    /// Get blocked IPs
    /// GET /api/dashboard/security/blocked-ips
    /// </summary>
    [HttpGet("security/blocked-ips")]
    public async Task<ActionResult<List<BlockedIpRecord>>> GetBlockedIps()
    {
        var blocked = await _db.BlockedIps
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.BlockedAt)
            .ToListAsync();

        return Ok(blocked);
    }

    /// <summary>
    /// Block an IP address
    /// POST /api/dashboard/security/block-ip
    /// </summary>
    [HttpPost("security/block-ip")]
    public async Task<ActionResult> BlockIp([FromBody] DashboardBlockIpRequest request)
    {
        var existing = await _db.BlockedIps.FirstOrDefaultAsync(b => b.IpAddress == request.IpAddress);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.Reason = request.Reason;
            existing.BlockedAt = DateTime.UtcNow;
            existing.ExpiresAt = request.DurationHours.HasValue 
                ? DateTime.UtcNow.AddHours(request.DurationHours.Value) 
                : null;
            existing.BlockedBy = request.BlockedBy;
        }
        else
        {
            _db.BlockedIps.Add(new BlockedIpRecord
            {
                IpAddress = request.IpAddress,
                Reason = request.Reason,
                ExpiresAt = request.DurationHours.HasValue 
                    ? DateTime.UtcNow.AddHours(request.DurationHours.Value) 
                    : null,
                BlockedBy = request.BlockedBy
            });
        }

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("IP_BLOCKED", "Security", request.IpAddress,
            null, request.BlockedBy, GetClientIp(), request.Reason, true);

        return Ok(new { message = $"IP {request.IpAddress} blocked" });
    }

    /// <summary>
    /// Unblock an IP address
    /// POST /api/dashboard/security/unblock-ip
    /// </summary>
    [HttpPost("security/unblock-ip")]
    public async Task<ActionResult> UnblockIp([FromBody] UnblockIpRequest request)
    {
        var blocked = await _db.BlockedIps.FirstOrDefaultAsync(b => b.IpAddress == request.IpAddress);

        if (blocked == null)
        {
            return NotFound(new { message = "IP not found in blocked list." });
        }

        blocked.IsActive = false;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("IP_UNBLOCKED", "Security", request.IpAddress,
            null, null, GetClientIp(), "Unblocked by admin", true);

        return Ok(new { message = $"IP {request.IpAddress} unblocked" });
    }

    // ==================== 2FA Endpoints ====================

    /// <summary>
    /// Setup 2FA for an admin user
    /// POST /api/dashboard/2fa/setup
    /// </summary>
    [HttpPost("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> Setup2FA([FromBody] DashboardSetup2FARequest request)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return NotFound(new { message = "Admin user not found." });
        }

        // Generate TOTP secret
        var secretBytes = new byte[20];
        System.Security.Cryptography.RandomNumberGenerator.Fill(secretBytes);
        var secret = Convert.ToBase64String(secretBytes);

        user.TwoFactorSecret = secret;
        await _db.SaveChangesAsync();

        // Generate QR code URL
        var encodedIssuer = Uri.EscapeDataString("FathomOS License Admin");
        var encodedEmail = Uri.EscapeDataString(request.Email);
        var otpauthUrl = $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
        var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(otpauthUrl)}";

        // Format for manual entry
        var manualKey = string.Join(" ", Enumerable.Range(0, (secret.Length + 3) / 4)
            .Select(i => secret.Substring(i * 4, Math.Min(4, secret.Length - i * 4))));

        return Ok(new TwoFactorSetupResponse
        {
            Secret = secret,
            QrCodeUrl = qrCodeUrl,
            ManualEntryKey = manualKey
        });
    }

    /// <summary>
    /// Enable 2FA after verifying code
    /// POST /api/dashboard/2fa/enable
    /// </summary>
    [HttpPost("2fa/enable")]
    public async Task<ActionResult> Enable2FA([FromBody] Enable2FARequest request)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return BadRequest(new { message = "2FA not set up. Call /2fa/setup first." });
        }

        // Verify TOTP code (simplified - use OtpNet in production)
        if (!VerifyTotpCode(user.TwoFactorSecret, request.Code))
        {
            return BadRequest(new { message = "Invalid verification code." });
        }

        // Generate backup codes
        var backupCodes = new List<string>();
        var bytes = new byte[4];
        for (int i = 0; i < 10; i++)
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var code = (BitConverter.ToUInt32(bytes) % 100000000).ToString("D8");
            backupCodes.Add($"{code[..4]}-{code[4..]}");
        }

        user.BackupCodes = System.Text.Json.JsonSerializer.Serialize(backupCodes);
        user.TwoFactorEnabled = true;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("2FA_ENABLED", "Admin", user.Id.ToString(),
            user.Id.ToString(), user.Email, GetClientIp(), "2FA enabled", true);

        return Ok(new { message = "2FA enabled successfully.", backupCodes });
    }

    /// <summary>
    /// Disable 2FA
    /// POST /api/dashboard/2fa/disable
    /// </summary>
    [HttpPost("2fa/disable")]
    public async Task<ActionResult> Disable2FA([FromBody] DashboardDisable2FARequest request)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return NotFound(new { message = "Admin user not found." });
        }

        // Verify password (simplified)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password + user.PasswordSalt));
        if (Convert.ToBase64String(hash) != user.PasswordHash)
        {
            return Unauthorized(new { message = "Invalid password." });
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.BackupCodes = null;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("2FA_DISABLED", "Admin", user.Id.ToString(),
            user.Id.ToString(), user.Email, GetClientIp(), "2FA disabled", true);

        return Ok(new { message = "2FA disabled." });
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

    private static bool VerifyTotpCode(string secret, string code)
    {
        try
        {
            var secretBytes = Convert.FromBase64String(secret);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

            // Check current and adjacent time windows
            for (int i = -1; i <= 1; i++)
            {
                var expectedCode = ComputeTotp(secretBytes, timestamp + i);
                if (expectedCode == code)
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeTotp(byte[] secret, long timestamp)
    {
        var timestampBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timestampBytes);

        var counter = new byte[8];
        Array.Copy(timestampBytes, 0, counter, 8 - timestampBytes.Length, timestampBytes.Length);

        using var hmac = new System.Security.Cryptography.HMACSHA1(secret);
        var hash = hmac.ComputeHash(counter);

        int offset = hash[^1] & 0x0F;
        int code = ((hash[offset] & 0x7F) << 24) |
                   ((hash[offset + 1] & 0xFF) << 16) |
                   ((hash[offset + 2] & 0xFF) << 8) |
                   (hash[offset + 3] & 0xFF);

        return (code % 1000000).ToString("D6");
    }
}

// ==================== Models ====================

public class DashboardOverviewStats
{
    public LicenseStatistics Licenses { get; set; } = new();
    public ActivationStatistics Activations { get; set; } = new();
    public TransferStatistics Transfers { get; set; } = new();
    public int ActiveSessions { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class LicenseStatistics
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Expired { get; set; }
    public int Revoked { get; set; }
    public int ExpiringThisWeek { get; set; }
    public int ExpiringThisMonth { get; set; }
    public int Online { get; set; }
    public int Offline { get; set; }
    public List<TierCount> ByTier { get; set; } = new();
}

public class TierCount
{
    public string Tier { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActivationStatistics
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int ThisMonth { get; set; }
    public int ThisWeek { get; set; }
}

public class TransferStatistics
{
    public int Total { get; set; }
    public int ThisMonth { get; set; }
}

public class TrendData
{
    public List<DailyCount> ActivationsPerDay { get; set; } = new();
    public List<DailyCount> LicensesCreatedPerDay { get; set; } = new();
    public List<DailyCount> ExpiringTimeline { get; set; } = new();
}

public class DailyCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class ExpiringLicense
{
    public string LicenseId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Edition { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
}

public class CreateBackupRequest
{
    public string? CreatedBy { get; set; }
    public string? Notes { get; set; }
    public string? BackupType { get; set; }
    public bool? Encrypt { get; set; }
}

public class RestoreBackupRequest
{
    public string? DecryptionKey { get; set; }
}

// ==================== Security & 2FA Models ====================

public class DashboardBlockIpRequest
{
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? DurationHours { get; set; }
    public string? BlockedBy { get; set; }
}

public class UnblockIpRequest
{
    public string IpAddress { get; set; } = string.Empty;
}

public class DashboardSetup2FARequest
{
    public string Email { get; set; } = string.Empty;
}

public class TwoFactorSetupResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
}

public class Enable2FARequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class DashboardDisable2FARequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TerminateSessionRequest
{
    public string? Reason { get; set; }
}
