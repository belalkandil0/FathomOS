using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.PersonnelManagement.Data;
using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Service for managing audit trail of record changes
/// </summary>
public class AuditService : IAuditService
{
    private readonly PersonnelDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditService(PersonnelDbContext context)
    {
        _context = context;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }

    /// <inheritdoc />
    public async Task<AuditLog> LogChangeAsync(
        string entityType,
        string entityId,
        string action,
        string? oldValues,
        string? newValues,
        string? changedBy,
        string? description = null)
    {
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedBy = changedBy,
            Description = description,
            ChangedAt = DateTime.UtcNow,
            SourceInfo = Environment.MachineName
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        return auditLog;
    }

    /// <inheritdoc />
    public async Task<AuditLog> LogChangeAsync<T>(
        string entityId,
        string action,
        T? oldEntity,
        T? newEntity,
        string? changedBy,
        string? description = null) where T : class
    {
        var entityType = typeof(T).Name;
        var oldValues = oldEntity != null ? JsonSerializer.Serialize(oldEntity, _jsonOptions) : null;
        var newValues = newEntity != null ? JsonSerializer.Serialize(newEntity, _jsonOptions) : null;

        return await LogChangeAsync(entityType, entityId, action, oldValues, newValues, changedBy, description);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByTypeAsync(string entityType, int skip = 0, int take = 100)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType)
            .OrderByDescending(a => a.ChangedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        string? entityType = null)
    {
        var query = _context.AuditLogs
            .Where(a => a.ChangedAt >= startDate && a.ChangedAt <= endDate);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        return await query
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string changedBy, int skip = 0, int take = 100)
    {
        return await _context.AuditLogs
            .Where(a => a.ChangedBy == changedBy)
            .OrderByDescending(a => a.ChangedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetAuditLogCountAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .CountAsync(a => a.EntityType == entityType && a.EntityId == entityId);
    }

    /// <inheritdoc />
    public async Task<int> PurgeOldLogsAsync(int retentionDays = 365)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var oldLogs = await _context.AuditLogs
            .Where(a => a.ChangedAt < cutoffDate)
            .ToListAsync();

        if (oldLogs.Count > 0)
        {
            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
        }

        return oldLogs.Count;
    }
}
