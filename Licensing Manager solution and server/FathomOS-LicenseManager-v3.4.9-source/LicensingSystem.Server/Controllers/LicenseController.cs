// LicensingSystem.Server/Controllers/LicenseController.cs
// ASP.NET Core API for license activation and validation

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LicensingSystem.Shared;
using LicensingSystem.Server.Services;
using LicensingSystem.Server.Data;

namespace LicensingSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly LicenseDbContext _db;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuditService _auditService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(
        ILicenseService licenseService, 
        LicenseDbContext db, 
        IRateLimitService rateLimitService,
        IAuditService auditService,
        ILogger<LicenseController> logger)
    {
        _licenseService = licenseService;
        _db = db;
        _rateLimitService = rateLimitService;
        _auditService = auditService;
        _logger = logger;
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
    /// Activates a license using a license key
    /// </summary>
    [HttpPost("activate")]
    public async Task<ActionResult<ActivationResponse>> Activate([FromBody] ActivationRequest request)
    {
        try
        {
            _logger.LogInformation("Activation request for key: {LicenseKey}, Email: {Email}", 
                MaskKey(request.LicenseKey), request.Email);

            var result = await _licenseService.ActivateLicenseAsync(request);

            if (!result.Success)
            {
                _logger.LogWarning("Activation failed: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Activation successful for: {Email}", request.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Activation error");
            return StatusCode(500, new ActivationResponse
            {
                Success = false,
                Message = "Internal server error. Please try again later."
            });
        }
    }

    /// <summary>
    /// Validates an existing license (heartbeat/renewal check)
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResponse>> Validate([FromBody] ValidationRequest request)
    {
        try
        {
            _logger.LogInformation("Validation request for license: {LicenseId}", request.LicenseId);
            
            var result = await _licenseService.ValidateLicenseAsync(request);
            
            _logger.LogInformation("Validation result for {LicenseId}: IsValid={IsValid}, Status={Status}, Message={Message}", 
                request.LicenseId, result.IsValid, result.Status, result.Message);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error for license: {LicenseId}", request.LicenseId);
            return StatusCode(500, new ValidationResponse
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = "Validation service unavailable."
            });
        }
    }

    /// <summary>
    /// Heartbeat endpoint - called periodically by clients to report status
    /// This allows tracking of active installations
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<HeartbeatResponse>> Heartbeat([FromBody] HeartbeatRequest request)
    {
        try
        {
            _logger.LogInformation("Heartbeat request for license: {LicenseId}, Hardware: {Hardware}", 
                request.LicenseId, request.HardwareFingerprint);
            
            // Find activation by license ID and hardware fingerprint
            var activation = await _db.LicenseActivations
                .Include(a => a.LicenseKey)
                .FirstOrDefaultAsync(a => 
                    a.LicenseKey.LicenseId == request.LicenseId && 
                    a.HardwareFingerprint == request.HardwareFingerprint &&
                    !a.IsDeactivated);

            if (activation == null)
            {
                _logger.LogWarning("Heartbeat: Exact match not found, trying hardware fingerprint only");
                // Try to find by hardware fingerprint alone
                activation = await _db.LicenseActivations
                    .Include(a => a.LicenseKey)
                    .FirstOrDefaultAsync(a => 
                        a.HardwareFingerprint == request.HardwareFingerprint &&
                        !a.IsDeactivated);
            }

            if (activation != null)
            {
                // Update last seen
                activation.LastSeenAt = DateTime.UtcNow;
                activation.AppVersion = request.AppVersion;
                activation.OsVersion = request.OsVersion;
                activation.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                await _db.SaveChangesAsync();

                // Check if license is still valid
                var isRevoked = activation.LicenseKey.IsRevoked;
                var isExpired = activation.LicenseKey.ExpiresAt < DateTime.UtcNow;
                
                // Determine license status string
                var licenseStatus = isRevoked ? "Revoked" : 
                                   isExpired ? "Expired" : "Valid";

                _logger.LogInformation("Heartbeat result for {LicenseId}: IsRevoked={IsRevoked}, IsExpired={IsExpired}, Status={Status}", 
                    request.LicenseId, isRevoked, isExpired, licenseStatus);

                return Ok(new HeartbeatResponse
                {
                    Success = true,
                    IsValid = !isRevoked && !isExpired,
                    IsRevoked = isRevoked,
                    IsExpired = isExpired,
                    ExpiresAt = activation.LicenseKey.ExpiresAt,
                    ServerTime = DateTime.UtcNow,
                    Message = isRevoked ? "License has been revoked" : 
                              isExpired ? "License has expired" : "OK",
                    // Enhanced fields
                    RevokedAt = isRevoked ? activation.LicenseKey.RevokedAt : null,
                    RevokeReason = isRevoked ? activation.LicenseKey.RevocationReason : null,
                    LicenseStatus = licenseStatus
                });
            }

            _logger.LogWarning("Heartbeat: Activation not found for {LicenseId}", request.LicenseId);
            return Ok(new HeartbeatResponse
            {
                Success = false,
                IsValid = false,
                Message = "Activation not found. Please reactivate."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat error for license: {LicenseId}", request.LicenseId);
            return Ok(new HeartbeatResponse
            {
                Success = false,
                Message = "Heartbeat service unavailable"
            });
        }
    }

    /// <summary>
    /// Registers an offline license with the server when internet becomes available
    /// This enables tracking of offline-generated licenses
    /// </summary>
    [HttpPost("sync-offline")]
    public async Task<ActionResult<OfflineSyncResponse>> SyncOfflineLicense([FromBody] OfflineSyncRequest request)
    {
        try
        {
            _logger.LogInformation("Offline sync request for license: {LicenseId}, Hardware: {Hardware}, Customer: {Customer}", 
                request.LicenseId, request.HardwareFingerprint, request.CustomerEmail);

            // Check if license already exists in database
            var existingLicense = await _db.LicenseKeys
                .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);

            if (existingLicense != null)
            {
                _logger.LogInformation("License {LicenseId} already exists, IsRevoked: {IsRevoked}", 
                    request.LicenseId, existingLicense.IsRevoked);
                    
                // License already registered, just update activation info
                var existingActivation = await _db.LicenseActivations
                    .FirstOrDefaultAsync(a => 
                        a.LicenseKeyId == existingLicense.Id && 
                        a.HardwareFingerprint == request.HardwareFingerprint);

                if (existingActivation == null)
                {
                    _logger.LogInformation("Adding new activation for existing license: {LicenseId}", request.LicenseId);
                    // Add new activation
                    var activation = new LicenseActivationRecord
                    {
                        LicenseKeyId = existingLicense.Id,
                        HardwareFingerprint = request.HardwareFingerprint,
                        ActivatedAt = request.ActivatedAt ?? DateTime.UtcNow,
                        LastSeenAt = DateTime.UtcNow,
                        AppVersion = request.AppVersion,
                        OsVersion = request.OsVersion,
                        MachineName = request.MachineName,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        IsOfflineActivation = true
                    };
                    _db.LicenseActivations.Add(activation);
                }
                else
                {
                    existingActivation.LastSeenAt = DateTime.UtcNow;
                    existingActivation.AppVersion = request.AppVersion;
                    existingActivation.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                }

                await _db.SaveChangesAsync();

                return Ok(new OfflineSyncResponse
                {
                    Success = true,
                    Message = "License sync successful",
                    IsRevoked = existingLicense.IsRevoked,
                    ExpiresAt = existingLicense.ExpiresAt
                });
            }

            // License doesn't exist - this is a new offline license, register it
            var newLicense = new LicenseKeyRecord
            {
                Key = request.LicenseKey ?? $"OFFLINE-{Guid.NewGuid():N}"[..19].ToUpperInvariant(),
                LicenseId = request.LicenseId,
                ProductName = request.ProductName ?? LicenseConstants.ProductName,
                Edition = request.Edition ?? "Professional",
                CustomerEmail = request.CustomerEmail ?? "",
                CustomerName = request.CustomerName ?? "",
                CreatedAt = request.CreatedAt ?? DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                SubscriptionType = SubscriptionType.Yearly,
                Features = request.Features ?? "Tier:Professional",
                IsOfflineGenerated = true
            };

            _db.LicenseKeys.Add(newLicense);
            await _db.SaveChangesAsync();

            // Add activation record
            var newActivation = new LicenseActivationRecord
            {
                LicenseKeyId = newLicense.Id,
                HardwareFingerprint = request.HardwareFingerprint,
                ActivatedAt = request.ActivatedAt ?? DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                AppVersion = request.AppVersion,
                OsVersion = request.OsVersion,
                MachineName = request.MachineName,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                IsOfflineActivation = true
            };
            _db.LicenseActivations.Add(newActivation);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Registered offline license: {LicenseId} for {Email}", 
                request.LicenseId, request.CustomerEmail);

            return Ok(new OfflineSyncResponse
            {
                Success = true,
                Message = "Offline license registered successfully",
                IsRevoked = false,
                ExpiresAt = newLicense.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Offline sync error");
            return StatusCode(500, new OfflineSyncResponse
            {
                Success = false,
                Message = "Failed to sync offline license"
            });
        }
    }

    /// <summary>
    /// Checks if a license is revoked
    /// </summary>
    [HttpGet("revoked/{licenseId}")]
    public async Task<ActionResult> CheckRevocation(string licenseId)
    {
        var isRevoked = await _licenseService.IsLicenseRevokedAsync(licenseId);
        return Ok(new { IsRevoked = isRevoked });
    }

    /// <summary>
    /// Gets server time (for client clock sync)
    /// </summary>
    [HttpGet("time")]
    public ActionResult GetServerTime()
    {
        return Ok(new { ServerTime = DateTime.UtcNow });
    }

    // ==================== SESSION MANAGEMENT ENDPOINTS ====================

    /// <summary>
    /// Start a license session (prevents concurrent use)
    /// POST /api/license/session/start
    /// </summary>
    [HttpPost("session/start")]
    public async Task<ActionResult<SessionStartResponse>> StartSession([FromBody] SessionStartRequest request)
    {
        var clientIp = GetClientIp();
        
        // Rate limiting - 10 attempts per 5 minutes
        if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "session-start", 10, TimeSpan.FromMinutes(5)))
        {
            await _auditService.LogAsync("RATE_LIMIT", "Session", request.LicenseId, null, null, clientIp,
                "Session start rate limit exceeded", false);
            return StatusCode(429, new SessionStartResponse
            {
                Success = false,
                Message = "Too many attempts. Please try again later."
            });
        }
        
        try
        {
            _logger.LogInformation("Session start request for license: {LicenseId}", request.LicenseId);

            // Find the license
            var license = await _db.LicenseKeys
                .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId && !l.IsRevoked);

            if (license == null)
            {
                return NotFound(new SessionStartResponse
                {
                    Success = false,
                    Message = "License not found or revoked"
                });
            }

            // Check for expired license
            if (license.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new SessionStartResponse
                {
                    Success = false,
                    Message = "License has expired"
                });
            }

            // Check for existing active session
            var existingSession = await _db.ActiveSessions
                .FirstOrDefaultAsync(s => s.LicenseId == request.LicenseId && s.IsActive);

            if (existingSession != null)
            {
                // Check if session is stale (no heartbeat for 5 minutes)
                var staleThreshold = DateTime.UtcNow.AddMinutes(-5);
                
                if (existingSession.LastHeartbeat < staleThreshold)
                {
                    // Session is stale, end it
                    existingSession.IsActive = false;
                    existingSession.EndedAt = DateTime.UtcNow;
                    existingSession.EndReason = "Stale session replaced";
                    _logger.LogInformation("Ended stale session for license: {LicenseId}", request.LicenseId);
                }
                else if (existingSession.HardwareFingerprint == request.HardwareFingerprint)
                {
                    // Same device, resume session
                    existingSession.LastHeartbeat = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    return Ok(new SessionStartResponse
                    {
                        Success = true,
                        SessionToken = existingSession.SessionToken,
                        Message = "Session resumed"
                    });
                }
                else
                {
                    // Different device, conflict
                    return Conflict(new SessionStartResponse
                    {
                        Success = false,
                        Message = "License is already in use on another device",
                        ConflictInfo = new SessionConflictInfo
                        {
                            ActiveDevice = existingSession.MachineName ?? "Unknown device",
                            LastSeen = existingSession.LastHeartbeat,
                            CanForceTerminate = true
                        }
                    });
                }
            }

            // Create new session
            var sessionToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var newSession = new ActiveSessionRecord
            {
                LicenseId = request.LicenseId,
                SessionToken = sessionToken,
                HardwareFingerprint = request.HardwareFingerprint,
                MachineName = request.MachineName,
                StartedAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                IsActive = true
            };

            _db.ActiveSessions.Add(newSession);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Session started for license: {LicenseId}, Machine: {Machine}", 
                request.LicenseId, request.MachineName);

            return Ok(new SessionStartResponse
            {
                Success = true,
                SessionToken = sessionToken,
                Message = "Session started"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session start error for license: {LicenseId}", request.LicenseId);
            return StatusCode(500, new SessionStartResponse
            {
                Success = false,
                Message = "Failed to start session"
            });
        }
    }

    /// <summary>
    /// Session heartbeat (keeps session alive)
    /// POST /api/license/session/heartbeat
    /// </summary>
    [HttpPost("session/heartbeat")]
    public async Task<ActionResult<SessionHeartbeatResponse>> SessionHeartbeat([FromBody] SessionHeartbeatRequest request)
    {
        try
        {
            var session = await _db.ActiveSessions
                .FirstOrDefaultAsync(s => s.SessionToken == request.SessionToken && s.IsActive);

            if (session == null)
            {
                return NotFound(new SessionHeartbeatResponse
                {
                    Success = false,
                    Message = "Session not found or expired"
                });
            }

            // Update heartbeat
            session.LastHeartbeat = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Check license status
            var license = await _db.LicenseKeys
                .FirstOrDefaultAsync(l => l.LicenseId == session.LicenseId);

            var isValid = license != null && !license.IsRevoked && license.ExpiresAt > DateTime.UtcNow;

            return Ok(new SessionHeartbeatResponse
            {
                Success = true,
                IsValid = isValid,
                ExpiresAt = license?.ExpiresAt,
                ServerTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session heartbeat error");
            return StatusCode(500, new SessionHeartbeatResponse
            {
                Success = false,
                Message = "Heartbeat failed"
            });
        }
    }

    /// <summary>
    /// End a session (on app close)
    /// POST /api/license/session/end
    /// </summary>
    [HttpPost("session/end")]
    public async Task<ActionResult> EndSession([FromBody] SessionEndRequest request)
    {
        try
        {
            var session = await _db.ActiveSessions
                .FirstOrDefaultAsync(s => s.SessionToken == request.SessionToken && s.IsActive);

            if (session != null)
            {
                session.IsActive = false;
                session.EndedAt = DateTime.UtcNow;
                session.EndReason = "Normal shutdown";
                await _db.SaveChangesAsync();

                _logger.LogInformation("Session ended for license: {LicenseId}", session.LicenseId);
            }

            return Ok(new { message = "Session ended" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session end error");
            return StatusCode(500, new { message = "Failed to end session" });
        }
    }

    /// <summary>
    /// Force terminate another session (for device conflict resolution)
    /// POST /api/license/session/force-terminate
    /// </summary>
    [HttpPost("session/force-terminate")]
    public async Task<ActionResult> ForceTerminateSession([FromBody] ForceTerminateRequest request)
    {
        try
        {
            _logger.LogInformation("Force terminate request for license: {LicenseId}", request.LicenseId);

            // Verify the requesting device has valid credentials
            var license = await _db.LicenseKeys
                .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);

            if (license == null)
            {
                return NotFound(new { message = "License not found" });
            }

            // Find and terminate existing session
            var existingSession = await _db.ActiveSessions
                .FirstOrDefaultAsync(s => s.LicenseId == request.LicenseId && s.IsActive);

            if (existingSession != null)
            {
                existingSession.IsActive = false;
                existingSession.EndedAt = DateTime.UtcNow;
                existingSession.EndReason = $"Force terminated by {request.MachineName}";
                await _db.SaveChangesAsync();

                _logger.LogWarning("Session force terminated for license: {LicenseId}, Previous device: {Device}", 
                    request.LicenseId, existingSession.MachineName);
            }

            return Ok(new { message = "Previous session terminated. You can now start a new session." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Force terminate error");
            return StatusCode(500, new { message = "Failed to terminate session" });
        }
    }

    /// <summary>
    /// Public endpoint to get license certificate info for display
    /// GET /api/license/certificate/{licenseId}
    /// </summary>
    [HttpGet("certificate/{licenseId}")]
    public async Task<ActionResult> GetLicenseCertificateInfo(string licenseId)
    {
        try
        {
            var license = await _db.LicenseKeys
                .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

            if (license == null)
            {
                return NotFound(new { error = "License not found" });
            }

            // Return public info only (no license key for online licenses)
            return Ok(new
            {
                licenseId = license.LicenseId,
                customerName = license.CustomerName,
                customerEmail = MaskEmail(license.CustomerEmail),
                edition = license.Edition,
                subscriptionType = license.SubscriptionType.ToString(),
                issuedAt = license.CreatedAt,
                expiresAt = license.ExpiresAt,
                brand = license.Brand,
                licenseeCode = license.LicenseeCode,
                supportCode = license.SupportCode,
                licenseType = license.LicenseType == "Offline" ? "Offline" : "Online",
                isRevoked = license.IsRevoked,
                modules = license.Features?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(f => f.StartsWith("Module:"))
                    .Select(f => f.Replace("Module:", ""))
                    .ToList() ?? new List<string>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license certificate info");
            return StatusCode(500, new { error = "Failed to retrieve license info" });
        }
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";
        
        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];
        
        var maskedName = name.Length <= 2 ? "**" : $"{name[0]}***{name[^1]}";
        return $"{maskedName}@{domain}";
    }

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 8)
            return "***";
        return $"{key[..4]}***{key[^4..]}";
    }
}

// Session Management DTOs

public class SessionStartRequest
{
    public string LicenseId { get; set; } = "";
    public string HardwareFingerprint { get; set; } = "";
    public string? MachineName { get; set; }
}

public class SessionStartResponse
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
    public SessionConflictInfo? ConflictInfo { get; set; }
}

public class SessionConflictInfo
{
    public string ActiveDevice { get; set; } = "";
    public DateTime LastSeen { get; set; }
    public bool CanForceTerminate { get; set; }
}

public class SessionHeartbeatRequest
{
    public string SessionToken { get; set; } = "";
}

public class SessionHeartbeatResponse
{
    public bool Success { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ServerTime { get; set; }
    public string? Message { get; set; }
}

public class SessionEndRequest
{
    public string SessionToken { get; set; } = "";
}

public class ForceTerminateRequest
{
    public string LicenseId { get; set; } = "";
    public string HardwareFingerprint { get; set; } = "";
    public string? MachineName { get; set; }
}

// Request/Response DTOs for new endpoints

public class HeartbeatRequest
{
    public string LicenseId { get; set; } = "";
    public string HardwareFingerprint { get; set; } = "";
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
}

public class HeartbeatResponse
{
    public bool Success { get; set; }
    public bool IsValid { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime ServerTime { get; set; }
    public string? Message { get; set; }
    
    // Enhanced fields for better diagnostics
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public string LicenseStatus { get; set; } = "Unknown";
}

public class OfflineSyncRequest
{
    public string LicenseId { get; set; } = "";
    public string? LicenseKey { get; set; }
    public string HardwareFingerprint { get; set; } = "";
    public string? ProductName { get; set; }
    public string? Edition { get; set; }
    public string? Features { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? AppVersion { get; set; }
    public string? OsVersion { get; set; }
    public string? MachineName { get; set; }
}

public class OfflineSyncResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
