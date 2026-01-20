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

// ==================== Session Management DTOs (for backwards compatibility) ====================
// Note: These are now inside the controller class for proper scoping

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

// ==================== MULTI-SEAT LICENSE ENDPOINTS ====================
// These endpoints support concurrent user licensing (multiple seats per license)

/// <summary>
/// Acquires a seat for the current session (multi-seat licensing)
/// POST /api/license/seats/acquire
/// </summary>
[HttpPost("seats/acquire")]
public async Task<ActionResult<AcquireSeatResponse>> AcquireSeat([FromBody] AcquireSeatRequest request)
{
    var clientIp = GetClientIp();

    // Rate limiting
    if (!await _rateLimitService.CheckRateLimitAsync(clientIp, "seats-acquire", 20, TimeSpan.FromMinutes(5)))
    {
        await _auditService.LogAsync("RATE_LIMIT", "Seat", request.LicenseId, null, null, clientIp,
            "Seat acquisition rate limit exceeded", false);
        return StatusCode(429, new AcquireSeatResponse
        {
            Success = false,
            Message = "Too many attempts. Please try again later."
        });
    }

    try
    {
        _logger.LogInformation("Seat acquisition request for license: {LicenseId}, Machine: {Machine}",
            request.LicenseId, request.MachineName);

        // Find the license
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId && !l.IsRevoked);

        if (license == null)
        {
            return NotFound(new AcquireSeatResponse
            {
                Success = false,
                Message = "License not found or revoked"
            });
        }

        // Check for expired license
        if (license.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new AcquireSeatResponse
            {
                Success = false,
                Message = "License has expired"
            });
        }

        // Get max seats from license features (default to 1 if not specified)
        var maxSeats = GetMaxSeatsFromLicense(license);

        // Get timeout from request or use default
        var timeoutMinutes = request.TimeoutMinutes > 0 ? request.TimeoutMinutes : 5;
        var staleThreshold = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        // Clean up stale sessions first
        var staleSessions = await _db.ActiveSessions
            .Where(s => s.LicenseId == request.LicenseId && s.IsActive && s.LastHeartbeat < staleThreshold)
            .ToListAsync();

        foreach (var stale in staleSessions)
        {
            stale.IsActive = false;
            stale.EndedAt = DateTime.UtcNow;
            stale.EndReason = "Session timeout";
        }

        // Count current active sessions
        var activeSessions = await _db.ActiveSessions
            .Where(s => s.LicenseId == request.LicenseId && s.IsActive)
            .ToListAsync();

        var seatsUsed = activeSessions.Count;

        // Check if this device already has a seat
        var existingSession = activeSessions
            .FirstOrDefault(s => s.HardwareFingerprint == request.HardwareFingerprint);

        if (existingSession != null)
        {
            // Same device, refresh the session
            existingSession.LastHeartbeat = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new AcquireSeatResponse
            {
                Success = true,
                SessionId = existingSession.SessionToken,
                SeatsUsed = seatsUsed,
                SeatsAvailable = maxSeats - seatsUsed,
                Message = "Session resumed"
            });
        }

        // Check if seats are available
        if (seatsUsed >= maxSeats)
        {
            var activeSessionsList = activeSessions.Select(s => new SeatActiveSession
            {
                SessionId = s.SessionToken,
                MachineName = s.MachineName ?? "Unknown",
                UserName = null, // Could be added if stored
                StartedAt = s.StartedAt,
                LastHeartbeat = s.LastHeartbeat,
                IsCurrentSession = false
            }).ToList();

            return Conflict(new AcquireSeatResponse
            {
                Success = false,
                SeatsUsed = seatsUsed,
                SeatsAvailable = 0,
                Message = $"All {maxSeats} license seats are currently in use.",
                ActiveSessions = activeSessionsList
            });
        }

        // Create new session
        var sessionToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var newSession = new ActiveSessionRecord
        {
            LicenseId = request.LicenseId,
            SessionToken = sessionToken,
            HardwareFingerprint = request.HardwareFingerprint,
            MachineName = request.MachineName,
            IpAddress = clientIp,
            StartedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
            IsActive = true
        };

        _db.ActiveSessions.Add(newSession);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("SEAT_ACQUIRE", "Session", request.LicenseId, null, null, clientIp,
            $"Seat acquired on {request.MachineName}", true);

        _logger.LogInformation("Seat acquired for license: {LicenseId}, Machine: {Machine}, Seats: {Used}/{Max}",
            request.LicenseId, request.MachineName, seatsUsed + 1, maxSeats);

        return Ok(new AcquireSeatResponse
        {
            Success = true,
            SessionId = sessionToken,
            SeatsUsed = seatsUsed + 1,
            SeatsAvailable = maxSeats - seatsUsed - 1,
            Message = "Seat acquired successfully"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Seat acquisition error for license: {LicenseId}", request.LicenseId);
        return StatusCode(500, new AcquireSeatResponse
        {
            Success = false,
            Message = "Failed to acquire seat"
        });
    }
}

/// <summary>
/// Releases a seat (multi-seat licensing)
/// POST /api/license/seats/release
/// </summary>
[HttpPost("seats/release")]
public async Task<ActionResult> ReleaseSeat([FromBody] SeatReleaseRequest request)
{
    try
    {
        _logger.LogInformation("Seat release request for session: {SessionId}", request.SessionId);

        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.SessionToken == request.SessionId && s.IsActive);

        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "Normal release";
            await _db.SaveChangesAsync();

            await _auditService.LogAsync("SEAT_RELEASE", "Session", session.LicenseId, null, null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                $"Seat released from {session.MachineName}", true);

            _logger.LogInformation("Seat released for license: {LicenseId}", session.LicenseId);
        }

        return Ok(new { success = true, message = "Seat released" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Seat release error");
        return StatusCode(500, new { success = false, message = "Failed to release seat" });
    }
}

/// <summary>
/// Heartbeat for multi-seat session
/// POST /api/license/seats/heartbeat
/// </summary>
[HttpPost("seats/heartbeat")]
public async Task<ActionResult<SeatHeartbeatResponse>> SeatHeartbeat([FromBody] SeatHeartbeatRequest request)
{
    try
    {
        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.SessionToken == request.SessionId && s.IsActive);

        if (session == null)
        {
            return NotFound(new SeatHeartbeatResponse
            {
                Success = false,
                Reason = "Session not found or expired",
                Message = "Session not found or expired. Please reacquire a seat."
            });
        }

        // Update heartbeat
        session.LastHeartbeat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Get seat count
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == session.LicenseId);

        var maxSeats = license != null ? GetMaxSeatsFromLicense(license) : 1;
        var seatsUsed = await _db.ActiveSessions
            .CountAsync(s => s.LicenseId == session.LicenseId && s.IsActive);

        // Check if license is still valid
        var isValid = license != null && !license.IsRevoked && license.ExpiresAt > DateTime.UtcNow;

        return Ok(new SeatHeartbeatResponse
        {
            Success = true,
            SeatsUsed = seatsUsed,
            TimeUntilExpiry = license != null ? license.ExpiresAt - DateTime.UtcNow : TimeSpan.Zero,
            Message = isValid ? "OK" : "License expired or revoked"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Seat heartbeat error");
        return StatusCode(500, new SeatHeartbeatResponse
        {
            Success = false,
            Message = "Heartbeat failed"
        });
    }
}

/// <summary>
/// Get seat status for a license
/// GET /api/license/seats/status?licenseId={id}
/// </summary>
[HttpGet("seats/status")]
public async Task<ActionResult<SeatStatusResponse>> GetSeatStatus([FromQuery] string licenseId)
{
    try
    {
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license == null)
        {
            return NotFound(new SeatStatusResponse
            {
                MaxSeats = 0,
                SeatsUsed = 0,
                Error = "License not found"
            });
        }

        var maxSeats = GetMaxSeatsFromLicense(license);
        var timeoutMinutes = 5;
        var staleThreshold = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        var activeSessions = await _db.ActiveSessions
            .Where(s => s.LicenseId == licenseId && s.IsActive && s.LastHeartbeat >= staleThreshold)
            .Select(s => new SeatActiveSession
            {
                SessionId = s.SessionToken,
                MachineName = s.MachineName ?? "Unknown",
                StartedAt = s.StartedAt,
                LastHeartbeat = s.LastHeartbeat,
                IsCurrentSession = false
            })
            .ToListAsync();

        return Ok(new SeatStatusResponse
        {
            MaxSeats = maxSeats,
            SeatsUsed = activeSessions.Count,
            ActiveSessions = activeSessions
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Get seat status error");
        return StatusCode(500, new SeatStatusResponse
        {
            Error = "Failed to get seat status"
        });
    }
}

/// <summary>
/// Get all active sessions for a license
/// GET /api/license/seats/sessions?licenseId={id}
/// </summary>
[HttpGet("seats/sessions")]
public async Task<ActionResult<List<SeatActiveSession>>> GetActiveSessions([FromQuery] string licenseId)
{
    try
    {
        var timeoutMinutes = 5;
        var staleThreshold = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

        var sessions = await _db.ActiveSessions
            .Where(s => s.LicenseId == licenseId && s.IsActive && s.LastHeartbeat >= staleThreshold)
            .Select(s => new SeatActiveSession
            {
                SessionId = s.SessionToken,
                MachineName = s.MachineName ?? "Unknown",
                StartedAt = s.StartedAt,
                LastHeartbeat = s.LastHeartbeat,
                IsCurrentSession = false
            })
            .ToListAsync();

        return Ok(sessions);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Get active sessions error");
        return Ok(new List<SeatActiveSession>());
    }
}

/// <summary>
/// Force release a session (admin function)
/// POST /api/license/seats/force-release
/// </summary>
[HttpPost("seats/force-release")]
public async Task<ActionResult> ForceReleaseSeat([FromBody] SeatReleaseRequest request)
{
    try
    {
        _logger.LogWarning("Force seat release request for session: {SessionId}", request.SessionId);

        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.SessionToken == request.SessionId && s.IsActive);

        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "Force released";
            await _db.SaveChangesAsync();

            await _auditService.LogAsync("SEAT_FORCE_RELEASE", "Session", session.LicenseId, null, null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                $"Seat force released from {session.MachineName}", true);

            _logger.LogWarning("Seat force released for license: {LicenseId}, Machine: {Machine}",
                session.LicenseId, session.MachineName);
        }

        return Ok(new { success = true, message = "Session force released" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Force seat release error");
        return StatusCode(500, new { success = false, message = "Failed to force release seat" });
    }
}

/// <summary>
/// Gets the maximum number of seats allowed for a license
/// </summary>
private int GetMaxSeatsFromLicense(LicenseKeyRecord license)
{
    // Check for MaxSeats in features (e.g., "MaxSeats:5")
    if (!string.IsNullOrEmpty(license.Features))
    {
        var features = license.Features.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var maxSeatsFeature = features.FirstOrDefault(f => f.Trim().StartsWith("MaxSeats:", StringComparison.OrdinalIgnoreCase));
        if (maxSeatsFeature != null)
        {
            var value = maxSeatsFeature.Split(':')[1].Trim();
            if (int.TryParse(value, out var seats))
                return seats;
        }
    }

    // Default seats by tier
    var tier = license.Edition?.ToLowerInvariant() ?? "professional";
    return tier switch
    {
        "basic" => 1,
        "professional" => 3,
        "enterprise" => 10,
        _ => 1
    };
}

// ==================== LICENSE TRANSFER ENDPOINTS ====================

/// <summary>
/// Initiate a license transfer by generating a transfer token
/// POST /api/license/transfer/initiate
/// </summary>
[HttpPost("transfer/initiate")]
public async Task<ActionResult<GenerateTransferResponse>> InitiateTransfer([FromBody] GenerateTransferRequest request)
{
    var clientIp = GetClientIp();

    try
    {
        _logger.LogInformation("Transfer initiation request for license: {LicenseId}", request.LicenseId);

        // Find the license
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId && !l.IsRevoked);

        if (license == null)
        {
            return NotFound(new GenerateTransferResponse
            {
                Success = false,
                ErrorCode = "LICENSE_NOT_FOUND",
                Message = "License not found or has been revoked"
            });
        }

        // Check if license is expired
        if (license.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new GenerateTransferResponse
            {
                Success = false,
                ErrorCode = "LICENSE_EXPIRED",
                Message = "Cannot transfer an expired license"
            });
        }

        // Check transfer limits (max 3 transfers per year for standard licenses)
        var maxTransfers = GetMaxTransfersForLicense(license);
        var transfersThisYear = await _db.LicenseTransfers
            .CountAsync(t => t.LicenseId == request.LicenseId &&
                           t.TransferredAt >= DateTime.UtcNow.AddYears(-1) &&
                           t.Status == "Completed");

        if (transfersThisYear >= maxTransfers)
        {
            return BadRequest(new GenerateTransferResponse
            {
                Success = false,
                ErrorCode = "TRANSFER_LIMIT_REACHED",
                Message = $"Maximum {maxTransfers} transfers per year. You have used {transfersThisYear}.",
                RemainingTransfers = 0
            });
        }

        // Check for pending transfers
        var pendingTransfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.LicenseId == request.LicenseId &&
                                     t.Status == "Pending" &&
                                     t.ExpiresAt > DateTime.UtcNow);

        if (pendingTransfer != null)
        {
            return BadRequest(new GenerateTransferResponse
            {
                Success = false,
                ErrorCode = "TRANSFER_PENDING",
                Message = "A transfer is already pending for this license",
                Token = pendingTransfer.TransferToken,
                ExpiresAt = pendingTransfer.ExpiresAt
            });
        }

        // Generate transfer token
        var transferToken = GenerateSecureToken();
        var validityHours = Math.Min(Math.Max(request.ValidityHours, 1), 72); // Between 1 and 72 hours

        var transfer = new LicenseTransferRecord
        {
            TransferId = Guid.NewGuid().ToString("N"),
            LicenseId = request.LicenseId,
            TransferToken = transferToken,
            SourceFingerprints = string.Join(",", request.SourceFingerprints),
            SourceMachineName = request.SourceMachineName,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(validityHours),
            Status = "Pending",
            TransferNumber = transfersThisYear + 1,
            InitiatedByIp = clientIp
        };

        _db.LicenseTransfers.Add(transfer);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("TRANSFER_INITIATED", "Transfer", request.LicenseId,
            null, transfer.TransferId, clientIp,
            $"Transfer initiated from {request.SourceMachineName}", true);

        _logger.LogInformation("Transfer initiated for license: {LicenseId}, Token: {Token}",
            request.LicenseId, transferToken[..8] + "...");

        return Ok(new GenerateTransferResponse
        {
            Success = true,
            Token = transferToken,
            ExpiresAt = transfer.ExpiresAt,
            TransferNumber = transfer.TransferNumber,
            RemainingTransfers = maxTransfers - transfersThisYear - 1
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error initiating transfer for license: {LicenseId}", request.LicenseId);
        return StatusCode(500, new GenerateTransferResponse
        {
            Success = false,
            ErrorCode = "SERVER_ERROR",
            Message = "An error occurred while initiating the transfer"
        });
    }
}

/// <summary>
/// Complete a license transfer on the target machine
/// POST /api/license/transfer/complete
/// </summary>
[HttpPost("transfer/complete")]
public async Task<ActionResult<CompleteTransferResponse>> CompleteTransfer([FromBody] CompleteTransferRequest request)
{
    var clientIp = GetClientIp();

    try
    {
        _logger.LogInformation("Transfer completion request with token: {Token}", request.Token[..Math.Min(8, request.Token.Length)] + "...");

        // Find the pending transfer
        var transfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.TransferToken == request.Token && t.Status == "Pending");

        if (transfer == null)
        {
            return NotFound(new CompleteTransferResponse
            {
                Success = false,
                ErrorCode = "TRANSFER_NOT_FOUND",
                Message = "Transfer token not found or already used"
            });
        }

        // Check if expired
        if (transfer.ExpiresAt < DateTime.UtcNow)
        {
            transfer.Status = "Expired";
            await _db.SaveChangesAsync();

            return BadRequest(new CompleteTransferResponse
            {
                Success = false,
                ErrorCode = "TRANSFER_EXPIRED",
                Message = "Transfer token has expired"
            });
        }

        // Find the license
        var license = await _db.LicenseKeys
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseId == transfer.LicenseId);

        if (license == null || license.IsRevoked)
        {
            return BadRequest(new CompleteTransferResponse
            {
                Success = false,
                ErrorCode = "LICENSE_INVALID",
                Message = "License is no longer valid"
            });
        }

        // Deactivate old activations
        foreach (var activation in license.Activations.Where(a => !a.IsDeactivated))
        {
            activation.IsDeactivated = true;
            activation.DeactivatedAt = DateTime.UtcNow;
            activation.DeactivationReason = "Transferred to new device";
        }

        // Create new activation for target machine
        var newActivation = new LicenseActivationRecord
        {
            LicenseKeyId = license.Id,
            HardwareFingerprint = request.TargetFingerprints.FirstOrDefault() ?? "",
            MachineName = request.TargetMachineName,
            ActivatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IpAddress = clientIp,
            AppVersion = "Transfer",
            OsVersion = "Unknown"
        };

        _db.LicenseActivations.Add(newActivation);

        // Update transfer record
        transfer.Status = "Completed";
        transfer.TargetFingerprints = string.Join(",", request.TargetFingerprints);
        transfer.TargetMachineName = request.TargetMachineName;
        transfer.TargetUserName = request.TargetUserName;
        transfer.TransferredAt = DateTime.UtcNow;
        transfer.CompletedByIp = clientIp;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("TRANSFER_COMPLETED", "Transfer", transfer.LicenseId,
            null, transfer.TransferId, clientIp,
            $"Transfer completed to {request.TargetMachineName}", true);

        _logger.LogInformation("Transfer completed for license: {LicenseId} to machine: {Machine}",
            transfer.LicenseId, request.TargetMachineName);

        return Ok(new CompleteTransferResponse
        {
            Success = true,
            LicenseId = transfer.LicenseId,
            TransferredAt = transfer.TransferredAt!.Value,
            Message = "License transferred successfully"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error completing transfer");
        return StatusCode(500, new CompleteTransferResponse
        {
            Success = false,
            ErrorCode = "SERVER_ERROR",
            Message = "An error occurred while completing the transfer"
        });
    }
}

/// <summary>
/// Get transfer status by transfer ID
/// GET /api/license/transfer/status/{transferId}
/// </summary>
[HttpGet("transfer/status/{transferId}")]
public async Task<ActionResult<TransferStatusResponse>> GetTransferStatus(string transferId)
{
    try
    {
        var transfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.TransferId == transferId || t.TransferToken == transferId);

        if (transfer == null)
        {
            return NotFound(new { message = "Transfer not found" });
        }

        return Ok(new TransferStatusResponse
        {
            TransferId = transfer.TransferId,
            Status = transfer.Status,
            ExpiresAt = transfer.ExpiresAt,
            SourceMachine = transfer.SourceMachineName,
            TargetMachine = transfer.TargetMachineName,
            RequestedAt = transfer.RequestedAt,
            CompletedAt = transfer.TransferredAt
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting transfer status");
        return StatusCode(500, new { message = "Error retrieving transfer status" });
    }
}

/// <summary>
/// Validate a transfer token
/// POST /api/license/transfer/validate
/// </summary>
[HttpPost("transfer/validate")]
public async Task<ActionResult<ValidateTransferResponse>> ValidateTransfer([FromBody] ValidateTransferRequest request)
{
    try
    {
        var transfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.TransferToken == request.Token && t.Status == "Pending");

        if (transfer == null)
        {
            return Ok(new ValidateTransferResponse
            {
                IsValid = false,
                ErrorCode = "TOKEN_NOT_FOUND",
                Message = "Transfer token not found or already used"
            });
        }

        if (transfer.ExpiresAt < DateTime.UtcNow)
        {
            return Ok(new ValidateTransferResponse
            {
                IsValid = false,
                ErrorCode = "TOKEN_EXPIRED",
                Message = "Transfer token has expired"
            });
        }

        return Ok(new ValidateTransferResponse
        {
            IsValid = true,
            ExpiresAt = transfer.ExpiresAt,
            SourceMachine = transfer.SourceMachineName,
            LicenseId = transfer.LicenseId
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating transfer token");
        return StatusCode(500, new ValidateTransferResponse
        {
            IsValid = false,
            ErrorCode = "SERVER_ERROR",
            Message = "Error validating transfer"
        });
    }
}

/// <summary>
/// Cancel a pending transfer
/// POST /api/license/transfer/cancel
/// </summary>
[HttpPost("transfer/cancel")]
public async Task<ActionResult> CancelTransfer([FromBody] CancelTransferRequest request)
{
    var clientIp = GetClientIp();

    try
    {
        var transfer = await _db.LicenseTransfers
            .FirstOrDefaultAsync(t => t.TransferToken == request.Token &&
                                     t.LicenseId == request.LicenseId &&
                                     t.Status == "Pending");

        if (transfer == null)
        {
            return NotFound(new { message = "Pending transfer not found" });
        }

        transfer.Status = "Cancelled";
        transfer.CancelledAt = DateTime.UtcNow;
        transfer.CancelledByIp = clientIp;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync("TRANSFER_CANCELLED", "Transfer", request.LicenseId,
            null, transfer.TransferId, clientIp, "Transfer cancelled by user", true);

        return Ok(new { message = "Transfer cancelled successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error cancelling transfer");
        return StatusCode(500, new { message = "Error cancelling transfer" });
    }
}

/// <summary>
/// Get transfer history for a license
/// GET /api/license/transfer/history/{licenseId}
/// </summary>
[HttpGet("transfer/history/{licenseId}")]
public async Task<ActionResult<List<TransferHistoryRecord>>> GetTransferHistory(string licenseId)
{
    try
    {
        var transfers = await _db.LicenseTransfers
            .Where(t => t.LicenseId == licenseId)
            .OrderByDescending(t => t.RequestedAt)
            .Select(t => new TransferHistoryRecord
            {
                TransferId = t.TransferId,
                TransferNumber = t.TransferNumber,
                SourceMachine = t.SourceMachineName ?? "Unknown",
                TargetMachine = t.TargetMachineName ?? "Pending",
                TransferredAt = t.TransferredAt ?? t.RequestedAt,
                Status = t.Status
            })
            .ToListAsync();

        return Ok(transfers);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting transfer history");
        return StatusCode(500, new List<TransferHistoryRecord>());
    }
}

/// <summary>
/// Get remaining transfers for a license
/// GET /api/license/transfer/remaining/{licenseId}
/// </summary>
[HttpGet("transfer/remaining/{licenseId}")]
public async Task<ActionResult<RemainingTransfersResponse>> GetRemainingTransfers(string licenseId)
{
    try
    {
        var license = await _db.LicenseKeys.FirstOrDefaultAsync(l => l.LicenseId == licenseId);
        if (license == null)
        {
            return NotFound(new { message = "License not found" });
        }

        var maxTransfers = GetMaxTransfersForLicense(license);
        var transfersThisYear = await _db.LicenseTransfers
            .CountAsync(t => t.LicenseId == licenseId &&
                           t.TransferredAt >= DateTime.UtcNow.AddYears(-1) &&
                           t.Status == "Completed");

        return Ok(new RemainingTransfersResponse
        {
            RemainingTransfers = maxTransfers - transfersThisYear,
            TotalTransfers = transfersThisYear,
            MaxTransfers = maxTransfers
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting remaining transfers");
        return StatusCode(500, new RemainingTransfersResponse());
    }
}

// ==================== FLOATING LICENSE POOL ENDPOINTS ====================

/// <summary>
/// Check out a license from the floating pool
/// POST /api/license/floating/checkout
/// </summary>
[HttpPost("floating/checkout")]
public async Task<ActionResult<FloatingPoolCheckoutResponse>> FloatingCheckout([FromBody] FloatingPoolCheckoutRequest request)
{
    var clientIp = GetClientIp();

    try
    {
        _logger.LogInformation("Floating checkout request for license: {LicenseId}", request.LicenseId);

        // Find the license
        var license = await _db.LicenseKeys
            .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId && !l.IsRevoked);

        if (license == null)
        {
            return NotFound(new FloatingPoolCheckoutResponse
            {
                Success = false,
                ErrorCode = "LICENSE_NOT_FOUND",
                Message = "License not found or revoked"
            });
        }

        // Check if expired
        if (license.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new FloatingPoolCheckoutResponse
            {
                Success = false,
                ErrorCode = "LICENSE_EXPIRED",
                Message = "License has expired"
            });
        }

        // Get pool size from license features
        var poolSize = GetFloatingPoolSize(license);
        if (poolSize <= 0)
        {
            return BadRequest(new FloatingPoolCheckoutResponse
            {
                Success = false,
                ErrorCode = "NOT_FLOATING_LICENSE",
                Message = "This license does not support floating pool"
            });
        }

        // Clean up expired checkouts
        var expiredCheckouts = await _db.FloatingPoolCheckouts
            .Where(c => c.LicenseId == request.LicenseId &&
                       c.IsActive &&
                       c.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var expired in expiredCheckouts)
        {
            expired.IsActive = false;
            expired.CheckedInAt = DateTime.UtcNow;
            expired.CheckInReason = "Expired";
        }

        // Check if user already has a checkout
        var existingCheckout = await _db.FloatingPoolCheckouts
            .FirstOrDefaultAsync(c => c.LicenseId == request.LicenseId &&
                                     c.HardwareFingerprint == request.HardwareFingerprint &&
                                     c.IsActive);

        if (existingCheckout != null)
        {
            // Extend existing checkout
            var extendDuration = request.RequestedDurationMinutes ?? 60;
            existingCheckout.ExpiresAt = DateTime.UtcNow.AddMinutes(extendDuration);
            existingCheckout.LastHeartbeat = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var activeCount = await _db.FloatingPoolCheckouts
                .CountAsync(c => c.LicenseId == request.LicenseId && c.IsActive);

            return Ok(new FloatingPoolCheckoutResponse
            {
                Success = true,
                CheckoutToken = existingCheckout.CheckoutToken,
                ExpiresAt = existingCheckout.ExpiresAt,
                PoolSize = poolSize,
                AvailableSlots = poolSize - activeCount,
                UsedSlots = activeCount,
                Message = "Existing checkout extended"
            });
        }

        // Count active checkouts
        var activeCheckouts = await _db.FloatingPoolCheckouts
            .CountAsync(c => c.LicenseId == request.LicenseId && c.IsActive);

        if (activeCheckouts >= poolSize)
        {
            // Return info about current users
            var users = await _db.FloatingPoolCheckouts
                .Where(c => c.LicenseId == request.LicenseId && c.IsActive)
                .Select(c => new FloatingPoolUser
                {
                    MachineName = c.MachineName,
                    UserName = c.UserName,
                    CheckedOutAt = c.CheckedOutAt,
                    ExpiresAt = c.ExpiresAt,
                    LastHeartbeat = c.LastHeartbeat
                })
                .ToListAsync();

            return BadRequest(new FloatingPoolCheckoutResponse
            {
                Success = false,
                ErrorCode = "POOL_EXHAUSTED",
                Message = $"All {poolSize} licenses are in use",
                PoolSize = poolSize,
                AvailableSlots = 0,
                UsedSlots = activeCheckouts
            });
        }

        // Create new checkout
        var checkoutDuration = request.RequestedDurationMinutes ?? 60;
        var checkout = new FloatingPoolCheckoutRecord
        {
            LicenseId = request.LicenseId,
            CheckoutToken = GenerateSecureToken(),
            HardwareFingerprint = request.HardwareFingerprint,
            MachineName = request.MachineName ?? "Unknown",
            UserName = request.UserName,
            CheckedOutAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(checkoutDuration),
            LastHeartbeat = DateTime.UtcNow,
            IsActive = true,
            IpAddress = clientIp
        };

        _db.FloatingPoolCheckouts.Add(checkout);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Floating checkout granted for license: {LicenseId}, Machine: {Machine}",
            request.LicenseId, request.MachineName);

        return Ok(new FloatingPoolCheckoutResponse
        {
            Success = true,
            CheckoutToken = checkout.CheckoutToken,
            ExpiresAt = checkout.ExpiresAt,
            PoolSize = poolSize,
            AvailableSlots = poolSize - activeCheckouts - 1,
            UsedSlots = activeCheckouts + 1
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during floating checkout");
        return StatusCode(500, new FloatingPoolCheckoutResponse
        {
            Success = false,
            ErrorCode = "SERVER_ERROR",
            Message = "An error occurred during checkout"
        });
    }
}

/// <summary>
/// Check in a license back to the floating pool
/// POST /api/license/floating/checkin
/// </summary>
[HttpPost("floating/checkin")]
public async Task<ActionResult> FloatingCheckin([FromBody] FloatingPoolCheckinRequest request)
{
    try
    {
        var checkout = await _db.FloatingPoolCheckouts
            .FirstOrDefaultAsync(c => c.LicenseId == request.LicenseId &&
                                     c.CheckoutToken == request.CheckoutToken &&
                                     c.IsActive);

        if (checkout == null)
        {
            return NotFound(new { success = false, message = "Checkout not found or already checked in" });
        }

        checkout.IsActive = false;
        checkout.CheckedInAt = DateTime.UtcNow;
        checkout.CheckInReason = "User check-in";

        await _db.SaveChangesAsync();

        _logger.LogInformation("Floating checkin for license: {LicenseId}, Machine: {Machine}",
            request.LicenseId, checkout.MachineName);

        return Ok(new { success = true, message = "License checked in successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during floating checkin");
        return StatusCode(500, new { success = false, message = "Error checking in license" });
    }
}

/// <summary>
/// Send heartbeat for floating license checkout
/// POST /api/license/floating/heartbeat
/// </summary>
[HttpPost("floating/heartbeat")]
public async Task<ActionResult> FloatingHeartbeat([FromBody] FloatingPoolCheckinRequest request)
{
    try
    {
        var checkout = await _db.FloatingPoolCheckouts
            .FirstOrDefaultAsync(c => c.LicenseId == request.LicenseId &&
                                     c.CheckoutToken == request.CheckoutToken &&
                                     c.IsActive);

        if (checkout == null)
        {
            return NotFound(new { success = false, message = "Checkout not found", shouldReacquire = true });
        }

        if (checkout.ExpiresAt < DateTime.UtcNow)
        {
            checkout.IsActive = false;
            checkout.CheckedInAt = DateTime.UtcNow;
            checkout.CheckInReason = "Expired";
            await _db.SaveChangesAsync();

            return BadRequest(new { success = false, message = "Checkout expired", shouldReacquire = true });
        }

        checkout.LastHeartbeat = DateTime.UtcNow;
        // Extend expiration on heartbeat
        checkout.ExpiresAt = DateTime.UtcNow.AddMinutes(15);

        await _db.SaveChangesAsync();

        return Ok(new {
            success = true,
            expiresAt = checkout.ExpiresAt,
            timeRemaining = (checkout.ExpiresAt - DateTime.UtcNow).TotalMinutes
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during floating heartbeat");
        return StatusCode(500, new { success = false, message = "Heartbeat failed" });
    }
}

/// <summary>
/// Get floating pool status for a license
/// GET /api/license/floating/status/{licenseId}
/// </summary>
[HttpGet("floating/status/{licenseId}")]
public async Task<ActionResult<FloatingPoolStatusResponse>> GetFloatingPoolStatus(string licenseId)
{
    try
    {
        var license = await _db.LicenseKeys.FirstOrDefaultAsync(l => l.LicenseId == licenseId);
        if (license == null)
        {
            return NotFound(new { message = "License not found" });
        }

        var poolSize = GetFloatingPoolSize(license);

        // Clean up expired checkouts first
        var expiredCheckouts = await _db.FloatingPoolCheckouts
            .Where(c => c.LicenseId == licenseId && c.IsActive && c.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var expired in expiredCheckouts)
        {
            expired.IsActive = false;
            expired.CheckedInAt = DateTime.UtcNow;
            expired.CheckInReason = "Expired";
        }
        await _db.SaveChangesAsync();

        var activeUsers = await _db.FloatingPoolCheckouts
            .Where(c => c.LicenseId == licenseId && c.IsActive)
            .Select(c => new FloatingPoolUser
            {
                CheckoutToken = c.CheckoutToken[..8] + "...",
                MachineName = c.MachineName,
                UserName = c.UserName,
                CheckedOutAt = c.CheckedOutAt,
                ExpiresAt = c.ExpiresAt,
                LastHeartbeat = c.LastHeartbeat
            })
            .ToListAsync();

        return Ok(new FloatingPoolStatusResponse
        {
            LicenseId = licenseId,
            PoolSize = poolSize,
            AvailableSlots = poolSize - activeUsers.Count,
            UsedSlots = activeUsers.Count,
            ActiveUsers = activeUsers
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting floating pool status");
        return StatusCode(500, new FloatingPoolStatusResponse { LicenseId = licenseId });
    }
}

// ==================== USAGE ANALYTICS ENDPOINTS ====================

/// <summary>
/// Sync usage data from client
/// POST /api/license/usage/sync
/// </summary>
[HttpPost("usage/sync")]
public async Task<ActionResult<UsageSyncResponse>> SyncUsage([FromBody] UsageSyncRequest request)
{
    try
    {
        _logger.LogInformation("Usage sync for license: {LicenseId}, Events: {Count}",
            request.LicenseId, request.Events.Count);

        var license = await _db.LicenseKeys.FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);
        if (license == null)
        {
            return NotFound(new UsageSyncResponse
            {
                Success = false,
                Message = "License not found"
            });
        }

        var processedCount = 0;
        foreach (var evt in request.Events)
        {
            var usageRecord = new UsageAnalyticsRecord
            {
                LicenseId = request.LicenseId,
                EventType = evt.EventType,
                EntityId = evt.EntityId,
                Timestamp = evt.Timestamp,
                MachineName = request.MachineName,
                Properties = evt.Properties != null ? System.Text.Json.JsonSerializer.Serialize(evt.Properties) : null,
                SyncedAt = DateTime.UtcNow
            };

            _db.UsageAnalytics.Add(usageRecord);
            processedCount++;
        }

        await _db.SaveChangesAsync();

        return Ok(new UsageSyncResponse
        {
            Success = true,
            EventsProcessed = processedCount,
            Message = $"Processed {processedCount} events"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error syncing usage data");
        return StatusCode(500, new UsageSyncResponse
        {
            Success = false,
            Message = "Error processing usage data"
        });
    }
}

/// <summary>
/// Get usage summary for a license
/// GET /api/license/usage/summary/{licenseId}
/// </summary>
[HttpGet("usage/summary/{licenseId}")]
public async Task<ActionResult<UsageSummaryResponse>> GetUsageSummary(
    string licenseId,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
{
    try
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var events = await _db.UsageAnalytics
            .Where(u => u.LicenseId == licenseId &&
                       u.Timestamp >= fromDate &&
                       u.Timestamp <= toDate)
            .ToListAsync();

        var moduleLaunches = events
            .Where(e => e.EventType == "ModuleLaunch" && e.EntityId != null)
            .GroupBy(e => e.EntityId!)
            .ToDictionary(g => g.Key, g => g.Count());

        var featureUsages = events
            .Where(e => e.EventType == "FeatureUsage" && e.EntityId != null)
            .GroupBy(e => e.EntityId!)
            .ToDictionary(g => g.Key, g => g.Count());

        var dailyUsage = events
            .GroupBy(e => e.Timestamp.Date.ToString("yyyy-MM-dd"))
            .Select(g => new DailyUsageSummary
            {
                Date = g.Key,
                Sessions = g.Count(e => e.EventType == "SessionStart"),
                UsageMinutes = g.Where(e => e.EventType == "SessionEnd")
                    .Sum(e => {
                        if (e.Properties != null)
                        {
                            try
                            {
                                var props = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(e.Properties);
                                if (props != null && props.TryGetValue("DurationMinutes", out var duration) && int.TryParse(duration, out var mins))
                                    return mins;
                            }
                            catch { }
                        }
                        return 0;
                    }),
                ModuleLaunches = g.Count(e => e.EventType == "ModuleLaunch"),
                FeatureUsages = g.Count(e => e.EventType == "FeatureUsage")
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Ok(new UsageSummaryResponse
        {
            LicenseId = licenseId,
            FromDate = fromDate,
            ToDate = toDate,
            TotalSessions = events.Count(e => e.EventType == "SessionStart"),
            TotalUsageMinutes = dailyUsage.Sum(d => d.UsageMinutes),
            TotalModuleLaunches = events.Count(e => e.EventType == "ModuleLaunch"),
            TotalFeatureUsages = events.Count(e => e.EventType == "FeatureUsage"),
            ModuleUsage = moduleLaunches,
            FeatureUsage = featureUsages,
            DailyUsage = dailyUsage
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting usage summary");
        return StatusCode(500, new UsageSummaryResponse { LicenseId = licenseId });
    }
}

// ==================== HELPER METHODS ====================

private int GetMaxTransfersForLicense(LicenseKeyRecord license)
{
    // Check for MaxTransfers in features
    if (!string.IsNullOrEmpty(license.Features))
    {
        var features = license.Features.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var maxTransfersFeature = features.FirstOrDefault(f => f.Trim().StartsWith("MaxTransfers:", StringComparison.OrdinalIgnoreCase));
        if (maxTransfersFeature != null)
        {
            var value = maxTransfersFeature.Split(':')[1].Trim();
            if (int.TryParse(value, out var transfers))
                return transfers;
        }
    }

    // Default by tier
    return license.Edition?.ToLowerInvariant() switch
    {
        "basic" => 2,
        "professional" => 3,
        "enterprise" => 5,
        _ => 3
    };
}

private int GetFloatingPoolSize(LicenseKeyRecord license)
{
    // Check for FloatingPool feature
    if (!string.IsNullOrEmpty(license.Features))
    {
        var features = license.Features.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var poolFeature = features.FirstOrDefault(f => f.Trim().StartsWith("FloatingPool:", StringComparison.OrdinalIgnoreCase));
        if (poolFeature != null)
        {
            var value = poolFeature.Split(':')[1].Trim();
            if (int.TryParse(value, out var size))
                return size;
        }
    }

    return 0; // 0 means not a floating license
}

private static string GenerateSecureToken()
{
    var bytes = new byte[32];
    using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes)
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');
}

} // End of LicenseController class

// ==================== MULTI-SEAT DTOs ====================

public class AcquireSeatRequest
{
    public string LicenseId { get; set; } = "";
    public string HardwareFingerprint { get; set; } = "";
    public string? MachineName { get; set; }
    public string? UserName { get; set; }
    public int TimeoutMinutes { get; set; } = 5;
}

public class AcquireSeatResponse
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public int SeatsUsed { get; set; }
    public int SeatsAvailable { get; set; }
    public string? Message { get; set; }
    public List<SeatActiveSession>? ActiveSessions { get; set; }
}

public class SeatReleaseRequest
{
    public string LicenseId { get; set; } = "";
    public string SessionId { get; set; } = "";
}

public class SeatHeartbeatRequest
{
    public string LicenseId { get; set; } = "";
    public string SessionId { get; set; } = "";
}

public class SeatHeartbeatResponse
{
    public bool Success { get; set; }
    public int SeatsUsed { get; set; }
    public TimeSpan TimeUntilExpiry { get; set; }
    public string? Message { get; set; }
    public string? Reason { get; set; }
}

public class SeatStatusResponse
{
    public int MaxSeats { get; set; }
    public int SeatsUsed { get; set; }
    public int SeatsAvailable => MaxSeats - SeatsUsed;
    public List<SeatActiveSession> ActiveSessions { get; set; } = new();
    public string? Error { get; set; }
}

public class SeatActiveSession
{
    public string SessionId { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string? UserName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public bool IsCurrentSession { get; set; }
}

// ==================== LICENSE TRANSFER DTOs ====================

public class GenerateTransferRequest
{
    public string LicenseId { get; set; } = "";
    public List<string> SourceFingerprints { get; set; } = new();
    public string SourceMachineName { get; set; } = "";
    public int ValidityHours { get; set; } = 24;
}

public class GenerateTransferResponse
{
    public bool Success { get; set; }
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public int TransferNumber { get; set; }
    public int RemainingTransfers { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}

public class ValidateTransferRequest
{
    public string Token { get; set; } = "";
}

public class ValidateTransferResponse
{
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? SourceMachine { get; set; }
    public string? LicenseId { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}

public class CompleteTransferRequest
{
    public string Token { get; set; } = "";
    public List<string> TargetFingerprints { get; set; } = new();
    public string TargetMachineName { get; set; } = "";
    public string? TargetUserName { get; set; }
}

public class CompleteTransferResponse
{
    public bool Success { get; set; }
    public string LicenseId { get; set; } = "";
    public string? NewLicenseData { get; set; }
    public DateTime TransferredAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}

public class CancelTransferRequest
{
    public string Token { get; set; } = "";
    public string LicenseId { get; set; } = "";
}

public class TransferHistoryRecord
{
    public string TransferId { get; set; } = "";
    public int TransferNumber { get; set; }
    public string SourceMachine { get; set; } = "";
    public string TargetMachine { get; set; } = "";
    public DateTime TransferredAt { get; set; }
    public string? InitiatedBy { get; set; }
    public string Status { get; set; } = "";
}

public class RemainingTransfersResponse
{
    public int RemainingTransfers { get; set; }
    public int TotalTransfers { get; set; }
    public int MaxTransfers { get; set; }
}

public class TransferStatusResponse
{
    public string TransferId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public string? SourceMachine { get; set; }
    public string? TargetMachine { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// ==================== FLOATING LICENSE POOL DTOs ====================

public class FloatingPoolCheckoutRequest
{
    public string LicenseId { get; set; } = "";
    public string HardwareFingerprint { get; set; } = "";
    public string? MachineName { get; set; }
    public string? UserName { get; set; }
    public int? RequestedDurationMinutes { get; set; }
}

public class FloatingPoolCheckoutResponse
{
    public bool Success { get; set; }
    public string? CheckoutToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int PoolSize { get; set; }
    public int AvailableSlots { get; set; }
    public int UsedSlots { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}

public class FloatingPoolCheckinRequest
{
    public string LicenseId { get; set; } = "";
    public string CheckoutToken { get; set; } = "";
}

public class FloatingPoolStatusResponse
{
    public string LicenseId { get; set; } = "";
    public int PoolSize { get; set; }
    public int AvailableSlots { get; set; }
    public int UsedSlots { get; set; }
    public List<FloatingPoolUser> ActiveUsers { get; set; } = new();
}

public class FloatingPoolUser
{
    public string CheckoutToken { get; set; } = "";
    public string? MachineName { get; set; }
    public string? UserName { get; set; }
    public DateTime CheckedOutAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
}

// ==================== USAGE ANALYTICS DTOs ====================

public class UsageSyncRequest
{
    public string LicenseId { get; set; } = "";
    public List<UsageEventRecord> Events { get; set; } = new();
    public string MachineName { get; set; } = "";
    public DateTime SyncedAt { get; set; }
}

public class UsageEventRecord
{
    public string EventType { get; set; } = "";
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}

public class UsageSyncResponse
{
    public bool Success { get; set; }
    public int EventsProcessed { get; set; }
    public string? Message { get; set; }
}

public class UsageSummaryResponse
{
    public string LicenseId { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalSessions { get; set; }
    public long TotalUsageMinutes { get; set; }
    public int TotalModuleLaunches { get; set; }
    public int TotalFeatureUsages { get; set; }
    public Dictionary<string, int> ModuleUsage { get; set; } = new();
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
    public List<DailyUsageSummary> DailyUsage { get; set; } = new();
}

public class DailyUsageSummary
{
    public string Date { get; set; } = "";
    public int Sessions { get; set; }
    public int UsageMinutes { get; set; }
    public int ModuleLaunches { get; set; }
    public int FeatureUsages { get; set; }
}
