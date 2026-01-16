using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

public class EquipmentCategory
{
    [Key]
    public Guid CategoryId { get; set; } = Guid.NewGuid();
    public Guid? ParentCategoryId { get; set; }
    [ForeignKey(nameof(ParentCategoryId))]
    [JsonIgnore]
    public virtual EquipmentCategory? ParentCategory { get; set; }
    [Required][MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required][MaxLength(20)]
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    [MaxLength(50)]
    public string? Icon { get; set; }
    [MaxLength(7)]
    public string? Color { get; set; }
    public bool IsConsumable { get; set; }
    public bool RequiresCertification { get; set; }
    public bool RequiresCalibration { get; set; }
    public int? DefaultCertificationPeriodDays { get; set; }
    public int? DefaultCalibrationPeriodDays { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public long SyncVersion { get; set; }
    [JsonIgnore]
    public virtual ICollection<EquipmentCategory> ChildCategories { get; set; } = new List<EquipmentCategory>();
}

public class EquipmentType
{
    [Key]
    public Guid TypeId { get; set; } = Guid.NewGuid();
    public Guid? CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    [JsonIgnore]
    public virtual EquipmentCategory? Category { get; set; }
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required][MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    [MaxLength(200)]
    public string? Manufacturer { get; set; }
    [MaxLength(200)]
    public string? Model { get; set; }
    [MaxLength(20)]
    public string? DefaultUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public long SyncVersion { get; set; }
}

public class Supplier
{
    [Key]
    public Guid SupplierId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(50)]
    public string? Code { get; set; }
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    [MaxLength(200)]
    public string? Email { get; set; }
    [MaxLength(50)]
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public long SyncVersion { get; set; }
}

public class Project
{
    [Key]
    public Guid ProjectId { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    [Required][MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required][MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? ClientName { get; set; }
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    [ForeignKey(nameof(LocationId))]
    [JsonIgnore]
    public virtual Location? Location { get; set; }
    public Guid? VesselId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    [MaxLength(100)]
    public string? ProjectManager { get; set; }
    public bool IsActive { get; set; } = true;
    public long SyncVersion { get; set; }
}

// Note: ProjectStatus enum is defined in Enums.cs

public class EquipmentPhoto
{
    [Key]
    public Guid PhotoId { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    [MaxLength(500)]
    public string PhotoUrl { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
    [MaxLength(200)]
    public string? Caption { get; set; }
    [MaxLength(20)]
    public string? PhotoType { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EquipmentDocument
{
    [Key]
    public Guid DocumentId { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    [MaxLength(500)]
    public string DocumentUrl { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? FileName { get; set; }
    [MaxLength(50)]
    public string? FileType { get; set; }
    [MaxLength(50)]
    public string? DocumentType { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EquipmentHistory
{
    [Key]
    public Guid HistoryId { get; set; } = Guid.NewGuid();
    
    public Guid EquipmentId { get; set; }
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    [Required]
    public HistoryAction Action { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    
    // Aliases for compatibility
    [NotMapped]
    public string? EventType => Action.ToString();
    [NotMapped]
    public string? EventDescription => Description;
    [NotMapped]
    public string? PreviousValue => OldValue;
    
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public Guid? ManifestId { get; set; }
    
    public Guid? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
    public long SyncVersion { get; set; }
}

/// <summary>
/// History action types for audit trail
/// </summary>
public enum HistoryAction
{
    Created,
    Updated,
    Deleted,
    StatusChanged,
    LocationChanged,
    Transferred,
    Certified,
    Calibrated,
    Serviced,
    PhotoAdded,
    PhotoRemoved,
    DocumentAdded,
    DocumentRemoved,
    Custom
}

public class Alert
{
    [Key]
    public Guid AlertId { get; set; } = Guid.NewGuid();
    [Required][MaxLength(50)]
    public string AlertType { get; set; } = string.Empty;
    public Guid? EquipmentId { get; set; }
    public Guid? ManifestId { get; set; }
    public Guid? LocationId { get; set; }
    [MaxLength(200)]
    public string? Title { get; set; }
    public string? Message { get; set; }
    [MaxLength(20)]
    public string Severity { get; set; } = "Info";
    public DateTime? DueDate { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MaintenanceRecord
{
    [Key]
    public Guid MaintenanceId { get; set; } = Guid.NewGuid();
    
    public Guid EquipmentId { get; set; }
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    [Required]
    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Preventive;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime PerformedDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(200)]
    public string? PerformedBy { get; set; }
    
    public DateTime? NextDueDate { get; set; }
    
    public decimal? Cost { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public Guid? ServiceProviderId { get; set; }
    
    [MaxLength(100)]
    public string? WorkOrderNumber { get; set; }
    
    public bool IsCompleted { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long SyncVersion { get; set; }
}

public class Certification
{
    [Key]
    public Guid CertificationId { get; set; } = Guid.NewGuid();
    
    public Guid EquipmentId { get; set; }
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    [MaxLength(100)]
    public string CertificateType { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }
    
    [MaxLength(200)]
    public string? CertifyingBody { get; set; }
    
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public byte[]? CertificateDocument { get; set; }
    
    [MaxLength(200)]
    public string? DocumentFileName { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long SyncVersion { get; set; }
    
    [NotMapped]
    public int DaysUntilExpiry => (ExpiryDate - DateTime.UtcNow).Days;
    
    [NotMapped]
    public bool IsExpired => ExpiryDate < DateTime.UtcNow;
    
    [NotMapped]
    public bool IsExpiringSoon => !IsExpired && DaysUntilExpiry <= 30;
}

public enum MaintenanceType
{
    Preventive,
    Corrective,
    Scheduled,
    Emergency,
    Inspection,
    Calibration,
    Certification,
    Service,
    Repair,
    Other
}

/// <summary>
/// Equipment event for tracking transfers, receipts, and other actions
/// </summary>
public class EquipmentEvent
{
    [Key]
    public Guid EventId { get; set; } = Guid.NewGuid();
    
    public Guid EquipmentId { get; set; }
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    [Required]
    public EventType EventType { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public Guid? LocationId { get; set; }
    [ForeignKey(nameof(LocationId))]
    [JsonIgnore]
    public virtual Location? Location { get; set; }
    
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    
    public Guid? RelatedManifestId { get; set; }
    
    public Guid? PerformedBy { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
    public long SyncVersion { get; set; }
}
