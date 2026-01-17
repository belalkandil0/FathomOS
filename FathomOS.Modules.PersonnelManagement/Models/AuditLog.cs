using System.ComponentModel.DataAnnotations;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents an audit log entry tracking changes to entities
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of entity that was changed (e.g., "Personnel", "Timesheet", "VesselAssignment")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the entity that was changed
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The action performed: "Create", "Update", or "Delete"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of the entity's state before the change (null for Create)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of the entity's state after the change (null for Delete)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Identifier or name of the user who made the change
    /// </summary>
    [MaxLength(200)]
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Timestamp when the change occurred (UTC)
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional description or reason for the change
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// IP address or machine name where the change originated
    /// </summary>
    [MaxLength(100)]
    public string? SourceInfo { get; set; }
}
