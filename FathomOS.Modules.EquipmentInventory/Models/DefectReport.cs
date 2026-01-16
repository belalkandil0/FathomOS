using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Models;

/// <summary>
/// Equipment Failure Notification (EFN) / Defect Report
/// Based on Subsea7 EFN Form FO-GL-ITS-EQP-003
/// </summary>
public class DefectReport
{
    [Key]
    public Guid DefectReportId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Auto-generated report number (EFN-2024-00001)
    /// </summary>
    [Required][MaxLength(50)]
    public string ReportNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// QR Code for scanning/tracking
    /// </summary>
    [MaxLength(100)]
    public string? QrCode { get; set; }
    
    #region Failure Details (Page 1)
    
    /// <summary>
    /// User who created this report
    /// </summary>
    public Guid CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    [JsonIgnore]
    public virtual User? CreatedByUser { get; set; }
    
    /// <summary>
    /// Date of failure/report creation
    /// </summary>
    [Required]
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Client/Project name
    /// </summary>
    [MaxLength(200)]
    public string? ClientProject { get; set; }
    
    /// <summary>
    /// Location where failure occurred
    /// </summary>
    public Guid? LocationId { get; set; }
    [ForeignKey(nameof(LocationId))]
    [JsonIgnore]
    public virtual Location? Location { get; set; }
    
    /// <summary>
    /// Third party location name (if not in system)
    /// </summary>
    [MaxLength(200)]
    public string? ThirdPartyLocationName { get; set; }
    
    /// <summary>
    /// ROV System identifier
    /// </summary>
    [MaxLength(100)]
    public string? RovSystem { get; set; }
    
    /// <summary>
    /// Working water depth in metres (approximate)
    /// </summary>
    public double? WorkingWaterDepthMetres { get; set; }
    
    /// <summary>
    /// Equipment Origin category
    /// </summary>
    [MaxLength(50)]
    public string? EquipmentOrigin { get; set; }
    
    /// <summary>
    /// Link to equipment item (if in inventory)
    /// </summary>
    public Guid? EquipmentId { get; set; }
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    /// <summary>
    /// Equipment category
    /// </summary>
    public Guid? EquipmentCategoryId { get; set; }
    [ForeignKey(nameof(EquipmentCategoryId))]
    [JsonIgnore]
    public virtual EquipmentCategory? EquipmentCategory { get; set; }
    
    /// <summary>
    /// Major component that failed
    /// </summary>
    [MaxLength(200)]
    public string? MajorComponent { get; set; }
    
    /// <summary>
    /// Minor component that failed
    /// </summary>
    [MaxLength(200)]
    public string? MinorComponent { get; set; }
    
    #endregion
    
    #region Equipment Details (Page 2)
    
    /// <summary>
    /// Internal or External ownership
    /// </summary>
    public EquipmentOwnership OwnershipType { get; set; } = EquipmentOwnership.Internal;
    
    /// <summary>
    /// Equipment owner name/company
    /// </summary>
    [MaxLength(200)]
    public string? EquipmentOwner { get; set; }
    
    /// <summary>
    /// Standard Supply or Project responsibility
    /// </summary>
    public ResponsibilityType ResponsibilityType { get; set; } = ResponsibilityType.Standard;
    
    /// <summary>
    /// SAP ID or Vendor Asset ID
    /// </summary>
    [MaxLength(100)]
    public string? SapIdOrVendorAssetId { get; set; }
    
    /// <summary>
    /// Serial number of failed equipment
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Manufacturer of failed equipment
    /// </summary>
    [MaxLength(100)]
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// Model of failed equipment
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; set; }
    
    #endregion
    
    #region Symptoms / Action Taken (Page 2)
    
    /// <summary>
    /// Category of fault
    /// </summary>
    [Required]
    public FaultCategory FaultCategory { get; set; }
    
    /// <summary>
    /// Detailed symptoms of fault
    /// </summary>
    [MaxLength(2000)]
    public string? DetailedSymptoms { get; set; }
    
    /// <summary>
    /// Photos of damage taken and attached
    /// </summary>
    public bool PhotosAttached { get; set; }
    
    /// <summary>
    /// Action taken to address the fault
    /// </summary>
    [MaxLength(2000)]
    public string? ActionTaken { get; set; }
    
    /// <summary>
    /// Parts available on board
    /// </summary>
    public bool PartsAvailableOnBoard { get; set; }
    
    /// <summary>
    /// Replacement required
    /// </summary>
    public bool ReplacementRequired { get; set; }
    
    /// <summary>
    /// Urgency of replacement
    /// </summary>
    public ReplacementUrgency ReplacementUrgency { get; set; } = ReplacementUrgency.Low;
    
    /// <summary>
    /// Further comments or recommendations
    /// </summary>
    [MaxLength(2000)]
    public string? FurtherComments { get; set; }
    
    /// <summary>
    /// Next port call or crew change date
    /// </summary>
    public DateTime? NextPortCallDate { get; set; }
    
    /// <summary>
    /// Next port call location
    /// </summary>
    [MaxLength(200)]
    public string? NextPortCallLocation { get; set; }
    
    /// <summary>
    /// Repair duration in minutes
    /// </summary>
    public int? RepairDurationMinutes { get; set; }
    
    /// <summary>
    /// Downtime duration in minutes
    /// </summary>
    public int? DowntimeDurationMinutes { get; set; }
    
    #endregion
    
    #region Workflow & Status
    
    /// <summary>
    /// Current status of the defect report
    /// </summary>
    public DefectReportStatus Status { get; set; } = DefectReportStatus.Draft;
    
    /// <summary>
    /// Date report was submitted
    /// </summary>
    public DateTime? SubmittedAt { get; set; }
    
    /// <summary>
    /// User who submitted the report
    /// </summary>
    public Guid? SubmittedByUserId { get; set; }
    [ForeignKey(nameof(SubmittedByUserId))]
    [JsonIgnore]
    public virtual User? SubmittedByUser { get; set; }
    
    /// <summary>
    /// User assigned to review/action this report
    /// </summary>
    public Guid? AssignedToUserId { get; set; }
    [ForeignKey(nameof(AssignedToUserId))]
    [JsonIgnore]
    public virtual User? AssignedToUser { get; set; }
    
    /// <summary>
    /// Date report was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }
    
    /// <summary>
    /// User who reviewed the report
    /// </summary>
    public Guid? ReviewedByUserId { get; set; }
    [ForeignKey(nameof(ReviewedByUserId))]
    [JsonIgnore]
    public virtual User? ReviewedByUser { get; set; }
    
    /// <summary>
    /// Review notes
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }
    
    /// <summary>
    /// Date defect was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
    
    /// <summary>
    /// User who resolved the defect
    /// </summary>
    public Guid? ResolvedByUserId { get; set; }
    [ForeignKey(nameof(ResolvedByUserId))]
    [JsonIgnore]
    public virtual User? ResolvedByUser { get; set; }
    
    /// <summary>
    /// Resolution notes
    /// </summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }
    
    /// <summary>
    /// Date report was closed
    /// </summary>
    public DateTime? ClosedAt { get; set; }
    
    /// <summary>
    /// User who closed the report
    /// </summary>
    public Guid? ClosedByUserId { get; set; }
    [ForeignKey(nameof(ClosedByUserId))]
    [JsonIgnore]
    public virtual User? ClosedByUser { get; set; }
    
    #endregion
    
    #region Metadata
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Sync status for offline support
    /// </summary>
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    
    /// <summary>
    /// Last synced timestamp
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
    
    #endregion
    
    #region Attachments & Parts
    
    /// <summary>
    /// Parts that failed or are required
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<DefectReportPart> Parts { get; set; } = new List<DefectReportPart>();
    
    /// <summary>
    /// Photo/document attachments
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<DefectReportAttachment> Attachments { get; set; } = new List<DefectReportAttachment>();
    
    /// <summary>
    /// History/audit trail
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<DefectReportHistory> History { get; set; } = new List<DefectReportHistory>();
    
    #endregion
    
    #region Computed Properties
    
    [NotMapped]
    public string StatusDisplay => Status.ToString();
    
    [NotMapped]
    public string UrgencyDisplay => ReplacementUrgency switch
    {
        ReplacementUrgency.High => "HIGH - Critical, within 24 hours",
        ReplacementUrgency.Medium => "MEDIUM - Alternative available, next port call",
        ReplacementUrgency.Low => "LOW - Spares on board",
        _ => "Unknown"
    };
    
    [NotMapped]
    public bool CanEdit => Status == DefectReportStatus.Draft || Status == DefectReportStatus.Returned;
    
    [NotMapped]
    public bool CanSubmit => Status == DefectReportStatus.Draft || Status == DefectReportStatus.Returned;
    
    [NotMapped]
    public bool CanReview => Status == DefectReportStatus.Submitted;
    
    [NotMapped]
    public bool CanResolve => Status == DefectReportStatus.UnderReview || Status == DefectReportStatus.InProgress;
    
    [NotMapped]
    public bool CanClose => Status == DefectReportStatus.Resolved;
    
    [NotMapped]
    public TimeSpan? TotalDowntime => DowntimeDurationMinutes.HasValue 
        ? TimeSpan.FromMinutes(DowntimeDurationMinutes.Value) 
        : null;
    
    [NotMapped]
    public TimeSpan? TotalRepairTime => RepairDurationMinutes.HasValue 
        ? TimeSpan.FromMinutes(RepairDurationMinutes.Value) 
        : null;
    
    #endregion
}

/// <summary>
/// Parts that failed or are required for repair
/// </summary>
public class DefectReportPart
{
    [Key]
    public Guid DefectReportPartId { get; set; } = Guid.NewGuid();
    
    public Guid DefectReportId { get; set; }
    [ForeignKey(nameof(DefectReportId))]
    [JsonIgnore]
    public virtual DefectReport? DefectReport { get; set; }
    
    /// <summary>
    /// Line number (1-15 per form)
    /// </summary>
    public int LineNumber { get; set; }
    
    /// <summary>
    /// Quantity that failed
    /// </summary>
    public int QuantityFailed { get; set; }
    
    /// <summary>
    /// Quantity required for replacement
    /// </summary>
    public int QuantityRequired { get; set; }
    
    /// <summary>
    /// SAP part number
    /// </summary>
    [MaxLength(50)]
    public string? SapNumber { get; set; }
    
    /// <summary>
    /// Part description
    /// </summary>
    [Required][MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Part model number
    /// </summary>
    [MaxLength(100)]
    public string? ModelNumber { get; set; }
    
    /// <summary>
    /// Part serial number
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// Link to inventory item if available
    /// </summary>
    public Guid? EquipmentId { get; set; }
    [ForeignKey(nameof(EquipmentId))]
    [JsonIgnore]
    public virtual Equipment? Equipment { get; set; }
    
    /// <summary>
    /// Whether part has been ordered
    /// </summary>
    public bool IsOrdered { get; set; }
    
    /// <summary>
    /// Whether part has been received
    /// </summary>
    public bool IsReceived { get; set; }
    
    /// <summary>
    /// Order reference number
    /// </summary>
    [MaxLength(100)]
    public string? OrderReference { get; set; }
}

/// <summary>
/// Attachments (photos, documents) for defect reports
/// </summary>
public class DefectReportAttachment
{
    [Key]
    public Guid AttachmentId { get; set; } = Guid.NewGuid();
    
    public Guid DefectReportId { get; set; }
    [ForeignKey(nameof(DefectReportId))]
    [JsonIgnore]
    public virtual DefectReport? DefectReport { get; set; }
    
    [Required][MaxLength(200)]
    public string FileName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? ContentType { get; set; }
    
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// File path or blob storage URL
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }
    
    /// <summary>
    /// Base64 encoded content for offline storage
    /// </summary>
    public string? Base64Content { get; set; }
    
    /// <summary>
    /// Description or caption
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Type of attachment
    /// </summary>
    public AttachmentType AttachmentType { get; set; } = AttachmentType.Photo;
    
    public Guid UploadedByUserId { get; set; }
    [ForeignKey(nameof(UploadedByUserId))]
    [JsonIgnore]
    public virtual User? UploadedByUser { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// History/audit trail for defect reports
/// </summary>
public class DefectReportHistory
{
    [Key]
    public Guid HistoryId { get; set; } = Guid.NewGuid();
    
    public Guid DefectReportId { get; set; }
    [ForeignKey(nameof(DefectReportId))]
    [JsonIgnore]
    public virtual DefectReport? DefectReport { get; set; }
    
    [Required][MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Details { get; set; }
    
    public DefectReportStatus? OldStatus { get; set; }
    public DefectReportStatus? NewStatus { get; set; }
    
    public Guid PerformedByUserId { get; set; }
    [ForeignKey(nameof(PerformedByUserId))]
    [JsonIgnore]
    public virtual User? PerformedByUser { get; set; }
    
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}

#region Enums

/// <summary>
/// Equipment ownership type
/// </summary>
public enum EquipmentOwnership
{
    Internal,
    External
}

/// <summary>
/// Responsibility type for equipment
/// </summary>
public enum ResponsibilityType
{
    Standard,
    Project
}

/// <summary>
/// Category of fault
/// </summary>
public enum FaultCategory
{
    [Display(Name = "Mechanical Failure")]
    MechanicalFailure,
    
    [Display(Name = "Electrical Failure")]
    ElectricalFailure,
    
    [Display(Name = "Hydraulic Failure")]
    HydraulicFailure,
    
    [Display(Name = "Software/Firmware Issue")]
    SoftwareIssue,
    
    [Display(Name = "Calibration Issue")]
    CalibrationIssue,
    
    [Display(Name = "Communication Failure")]
    CommunicationFailure,
    
    [Display(Name = "Physical Damage")]
    PhysicalDamage,
    
    [Display(Name = "Corrosion/Wear")]
    CorrosionWear,
    
    [Display(Name = "Leak")]
    Leak,
    
    [Display(Name = "Overheating")]
    Overheating,
    
    [Display(Name = "Vibration/Noise")]
    VibrationNoise,
    
    [Display(Name = "Sensor Malfunction")]
    SensorMalfunction,
    
    [Display(Name = "Power Supply Issue")]
    PowerSupplyIssue,
    
    [Display(Name = "Connector/Cable Issue")]
    ConnectorCableIssue,
    
    [Display(Name = "User Error")]
    UserError,
    
    [Display(Name = "Unknown")]
    Unknown,
    
    [Display(Name = "Other")]
    Other
}

/// <summary>
/// Urgency level for replacement
/// </summary>
public enum ReplacementUrgency
{
    [Display(Name = "HIGH - Critical, within 24 hours")]
    High,
    
    [Display(Name = "MEDIUM - Alternative available, next port call")]
    Medium,
    
    [Display(Name = "LOW - Spares on board")]
    Low
}

/// <summary>
/// Status of defect report workflow
/// </summary>
public enum DefectReportStatus
{
    [Display(Name = "Draft")]
    Draft,
    
    [Display(Name = "Submitted")]
    Submitted,
    
    [Display(Name = "Under Review")]
    UnderReview,
    
    [Display(Name = "Returned")]
    Returned,
    
    [Display(Name = "In Progress")]
    InProgress,
    
    [Display(Name = "Resolved")]
    Resolved,
    
    [Display(Name = "Closed")]
    Closed,
    
    [Display(Name = "Cancelled")]
    Cancelled
}

/// <summary>
/// Type of attachment
/// </summary>
public enum AttachmentType
{
    Photo,
    Document,
    Video,
    Report,
    Other
}

/// <summary>
/// Equipment origin categories (from EFN form)
/// </summary>
public static class EquipmentOriginCategories
{
    public static readonly string[] GeneralVesselRov = new[]
    {
        "Modular Handling System",
        "ROV",
        "Simulator",
        "Tooling",
        "Vessel / Rig"
    };
    
    public static readonly string[] SurveyInspection = new[]
    {
        "Survey & Inspection"
    };
    
    public static string[] GetAll() => GeneralVesselRov.Concat(SurveyInspection).ToArray();
}

#endregion
