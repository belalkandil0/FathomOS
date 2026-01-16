// LicensingSystem.Server/Services/SessionService.cs
// Active session tracking with heartbeat for concurrent use prevention

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.Security.Cryptography;

namespace LicensingSystem.Server.Services;

public interface ISessionService
{
    Task<SessionStartResult> StartSessionAsync(string licenseId, string hardwareFingerprint, 
        string? machineName, string? ipAddress, string? appVersion);
    
    Task<bool> HeartbeatAsync(string sessionToken);
    Task EndSessionAsync(string sessionToken, string reason = "Logout");
    Task<ActiveSessionRecord?> GetActiveSessionAsync(string licenseId);
    Task<List<ActiveSessionRecord>> GetActiveSessionsAsync();
    Task TerminateSessionAsync(string licenseId, string reason = "Terminated");
    Task CleanupInactiveSessionsAsync(TimeSpan timeout);
}

public class SessionService : ISessionService
{
    private readonly LicenseDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ILogger<SessionService> _logger;
    
    // Session timeout - if no heartbeat in this time, session is considered inactive
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5);

    public SessionService(
        LicenseDbContext db, 
        IAuditService auditService,
        ILogger<SessionService> logger)
    {
        _db = db;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<SessionStartResult> StartSessionAsync(string licenseId, string hardwareFingerprint, 
        string? machineName, string? ipAddress, string? appVersion)
    {
        // Check for existing active session
        var existingSession = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.LicenseId == licenseId && s.IsActive);

        if (existingSession != null)
        {
            // Check if session is stale (no heartbeat)
            if (existingSession.LastHeartbeat < DateTime.UtcNow - SessionTimeout)
            {
                // Auto-terminate stale session
                existingSession.IsActive = false;
                existingSession.EndedAt = DateTime.UtcNow;
                existingSession.EndReason = "Timeout";
                
                _logger.LogInformation("Terminated stale session for license {LicenseId}", licenseId);
            }
            else
            {
                // Check if it's the same device
                if (existingSession.HardwareFingerprint == hardwareFingerprint)
                {
                    // Same device - update existing session
                    existingSession.LastHeartbeat = DateTime.UtcNow;
                    existingSession.IpAddress = ipAddress;
                    existingSession.AppVersion = appVersion;
                    await _db.SaveChangesAsync();

                    return new SessionStartResult
                    {
                        Success = true,
                        SessionToken = existingSession.SessionToken,
                        Message = "Session resumed on same device",
                        IsExistingSession = true
                    };
                }
                else
                {
                    // Different device - deny new session
                    return new SessionStartResult
                    {
                        Success = false,
                        Message = "License is currently in use on another device",
                        ActiveDevice = existingSession.MachineName,
                        LastSeen = existingSession.LastHeartbeat,
                        CanForceTerminate = true
                    };
                }
            }
        }

        // Create new session
        var sessionToken = GenerateSessionToken();
        var newSession = new ActiveSessionRecord
        {
            LicenseId = licenseId,
            SessionToken = sessionToken,
            HardwareFingerprint = hardwareFingerprint,
            MachineName = machineName,
            IpAddress = ipAddress,
            AppVersion = appVersion,
            StartedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
            IsActive = true
        };

        _db.ActiveSessions.Add(newSession);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("SESSION_STARTED", "Session", licenseId, null, null, ipAddress,
            $"Session started on {machineName}", true);

        _logger.LogInformation("Started new session for license {LicenseId} on {MachineName}", 
            licenseId, machineName);

        return new SessionStartResult
        {
            Success = true,
            SessionToken = sessionToken,
            Message = "Session started successfully",
            IsExistingSession = false
        };
    }

    public async Task<bool> HeartbeatAsync(string sessionToken)
    {
        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session == null)
        {
            _logger.LogWarning("Heartbeat received for invalid/inactive session: {Token}", 
                sessionToken[..Math.Min(8, sessionToken.Length)] + "***");
            return false;
        }

        session.LastHeartbeat = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task EndSessionAsync(string sessionToken, string reason = "Logout")
    {
        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = reason;
            await _db.SaveChangesAsync();

            await _auditService.LogAsync("SESSION_ENDED", "Session", session.LicenseId, null, null, session.IpAddress,
                $"Session ended: {reason}", true);

            _logger.LogInformation("Ended session for license {LicenseId}: {Reason}", 
                session.LicenseId, reason);
        }
    }

    public async Task<ActiveSessionRecord?> GetActiveSessionAsync(string licenseId)
    {
        return await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.LicenseId == licenseId && s.IsActive);
    }

    public async Task<List<ActiveSessionRecord>> GetActiveSessionsAsync()
    {
        return await _db.ActiveSessions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastHeartbeat)
            .ToListAsync();
    }

    public async Task TerminateSessionAsync(string licenseId, string reason = "Terminated")
    {
        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.LicenseId == licenseId && s.IsActive);

        if (session != null)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = reason;
            await _db.SaveChangesAsync();

            await _auditService.LogAsync("SESSION_TERMINATED", "Session", licenseId, null, null, null,
                $"Session terminated: {reason}", true);

            _logger.LogWarning("Terminated session for license {LicenseId}: {Reason}", licenseId, reason);
        }
    }

    public async Task CleanupInactiveSessionsAsync(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;

        var staleSessions = await _db.ActiveSessions
            .Where(s => s.IsActive && s.LastHeartbeat < cutoff)
            .ToListAsync();

        foreach (var session in staleSessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "Timeout";
        }

        await _db.SaveChangesAsync();

        if (staleSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} inactive sessions", staleSessions.Count);
        }
    }

    private static string GenerateSessionToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

public class SessionStartResult
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
    public bool IsExistingSession { get; set; }
    public string? ActiveDevice { get; set; }
    public DateTime? LastSeen { get; set; }
    public bool CanForceTerminate { get; set; }
}
