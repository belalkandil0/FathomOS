using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Interface for audit logging operations
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs a change to an entity
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., "Personnel", "Timesheet")</param>
    /// <param name="entityId">Identifier of the entity</param>
    /// <param name="action">The action performed: "Create", "Update", or "Delete"</param>
    /// <param name="oldValues">JSON of the entity state before change (null for Create)</param>
    /// <param name="newValues">JSON of the entity state after change (null for Delete)</param>
    /// <param name="changedBy">User who made the change</param>
    /// <param name="description">Optional description of the change</param>
    /// <returns>The created audit log entry</returns>
    Task<AuditLog> LogChangeAsync(
        string entityType,
        string entityId,
        string action,
        string? oldValues,
        string? newValues,
        string? changedBy,
        string? description = null);

    /// <summary>
    /// Logs a change to an entity using objects instead of JSON strings
    /// </summary>
    /// <typeparam name="T">Type of the entity</typeparam>
    /// <param name="entityId">Identifier of the entity</param>
    /// <param name="action">The action performed: "Create", "Update", or "Delete"</param>
    /// <param name="oldEntity">Entity state before change (null for Create)</param>
    /// <param name="newEntity">Entity state after change (null for Delete)</param>
    /// <param name="changedBy">User who made the change</param>
    /// <param name="description">Optional description of the change</param>
    /// <returns>The created audit log entry</returns>
    Task<AuditLog> LogChangeAsync<T>(
        string entityId,
        string action,
        T? oldEntity,
        T? newEntity,
        string? changedBy,
        string? description = null) where T : class;

    /// <summary>
    /// Gets the audit log history for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">Identifier of the entity</param>
    /// <returns>List of audit log entries for the entity, ordered by date descending</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogAsync(string entityType, string entityId);

    /// <summary>
    /// Gets all audit logs for a specific entity type
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="skip">Number of records to skip (for pagination)</param>
    /// <param name="take">Number of records to take (for pagination)</param>
    /// <returns>List of audit log entries</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsByTypeAsync(string entityType, int skip = 0, int take = 100);

    /// <summary>
    /// Gets audit logs within a date range
    /// </summary>
    /// <param name="startDate">Start of the date range (UTC)</param>
    /// <param name="endDate">End of the date range (UTC)</param>
    /// <param name="entityType">Optional entity type filter</param>
    /// <returns>List of audit log entries within the date range</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        string? entityType = null);

    /// <summary>
    /// Gets audit logs for changes made by a specific user
    /// </summary>
    /// <param name="changedBy">User identifier</param>
    /// <param name="skip">Number of records to skip (for pagination)</param>
    /// <param name="take">Number of records to take (for pagination)</param>
    /// <returns>List of audit log entries by the user</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string changedBy, int skip = 0, int take = 100);

    /// <summary>
    /// Gets the total count of audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">Identifier of the entity</param>
    /// <returns>Count of audit log entries</returns>
    Task<int> GetAuditLogCountAsync(string entityType, string entityId);

    /// <summary>
    /// Deletes old audit logs beyond a specified retention period
    /// </summary>
    /// <param name="retentionDays">Number of days to retain logs</param>
    /// <returns>Number of deleted log entries</returns>
    Task<int> PurgeOldLogsAsync(int retentionDays = 365);
}
