// LicensingSystem.Server/Services/AuditService.cs
// Comprehensive audit logging service

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.Text.Json;

namespace LicensingSystem.Server.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType, string? entityId, string? userId, string? userEmail, 
        string? ipAddress, string? details, bool success, string? oldValues = null, string? newValues = null);
    
    Task<List<AuditLogRecord>> GetLogsAsync(int page = 1, int pageSize = 100, string? action = null, 
        string? entityType = null, DateTime? from = null, DateTime? to = null);
    
    Task<List<AuditLogRecord>> GetEntityHistoryAsync(string entityType, string entityId);
    Task<List<AuditLogRecord>> GetUserActivityAsync(string userEmail, int days = 30);
}

public class AuditService : IAuditService
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        LicenseDbContext db, 
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string? entityType, string? entityId, string? userId, 
        string? userEmail, string? ipAddress, string? details, bool success, 
        string? oldValues = null, string? newValues = null)
    {
        try
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();

            var log = new AuditLogRecord
            {
                Timestamp = DateTime.UtcNow,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                UserEmail = userEmail,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OldValues = oldValues,
                NewValues = newValues,
                Details = details,
                Success = success
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();

            // Also log to application logs for quick debugging
            if (success)
            {
                _logger.LogInformation("AUDIT: {Action} on {EntityType}/{EntityId} by {UserEmail} from {IpAddress}: {Details}",
                    action, entityType, entityId, userEmail, ipAddress, details);
            }
            else
            {
                _logger.LogWarning("AUDIT FAILED: {Action} on {EntityType}/{EntityId} by {UserEmail} from {IpAddress}: {Details}",
                    action, entityType, entityId, userEmail, ipAddress, details);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
        }
    }

    public async Task<List<AuditLogRecord>> GetLogsAsync(int page = 1, int pageSize = 100, 
        string? action = null, string? entityType = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<AuditLogRecord>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        return await _db.AuditLogs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<AuditLogRecord>> GetUserActivityAsync(string userEmail, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        
        return await _db.AuditLogs
            .Where(l => l.UserEmail == userEmail && l.Timestamp >= since)
            .OrderByDescending(l => l.Timestamp)
            .Take(500)
            .ToListAsync();
    }
}
