using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents a deliverable within a project.
/// </summary>
public class ProjectDeliverable
{
    #region Identification

    [Key]
    public Guid DeliverableId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the project
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the project
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    [JsonIgnore]
    public virtual SurveyProject? Project { get; set; }

    /// <summary>
    /// Foreign key to associated milestone (optional)
    /// </summary>
    public Guid? MilestoneId { get; set; }

    /// <summary>
    /// Navigation property to the milestone
    /// </summary>
    [ForeignKey(nameof(MilestoneId))]
    [JsonIgnore]
    public virtual ProjectMilestone? Milestone { get; set; }

    /// <summary>
    /// Deliverable number/code (e.g., D001, RPT-001)
    /// </summary>
    [MaxLength(50)]
    public string? DeliverableNumber { get; set; }

    /// <summary>
    /// Client's document number/reference
    /// </summary>
    [MaxLength(100)]
    public string? ClientReference { get; set; }

    #endregion

    #region Description

    /// <summary>
    /// Deliverable name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the deliverable
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of deliverable
    /// </summary>
    public DeliverableType Type { get; set; } = DeliverableType.Other;

    /// <summary>
    /// Format of the deliverable
    /// </summary>
    public DeliverableFormat Format { get; set; } = DeliverableFormat.PDF;

    /// <summary>
    /// Priority level
    /// </summary>
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    #endregion

    #region Status

    /// <summary>
    /// Current status of the deliverable
    /// </summary>
    public DeliverableStatus Status { get; set; } = DeliverableStatus.NotStarted;

    /// <summary>
    /// Completion percentage (0-100)
    /// </summary>
    public decimal PercentComplete { get; set; }

    /// <summary>
    /// Current revision number
    /// </summary>
    [MaxLength(10)]
    public string? RevisionNumber { get; set; } = "A";

    /// <summary>
    /// Number of revisions made
    /// </summary>
    public int RevisionCount { get; set; }

    #endregion

    #region Schedule

    /// <summary>
    /// Planned due date
    /// </summary>
    public DateTime? PlannedDueDate { get; set; }

    /// <summary>
    /// Forecasted completion date
    /// </summary>
    public DateTime? ForecastDate { get; set; }

    /// <summary>
    /// Actual submission date
    /// </summary>
    public DateTime? SubmissionDate { get; set; }

    /// <summary>
    /// Date approved by client
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Original baseline date
    /// </summary>
    public DateTime? BaselineDate { get; set; }

    #endregion

    #region File Information

    /// <summary>
    /// File name of the deliverable
    /// </summary>
    [MaxLength(255)]
    public string? FileName { get; set; }

    /// <summary>
    /// File path or URL to the deliverable
    /// </summary>
    [MaxLength(1000)]
    public string? FilePath { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// File hash for integrity verification
    /// </summary>
    [MaxLength(64)]
    public string? FileHash { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [MaxLength(100)]
    public string? MimeType { get; set; }

    #endregion

    #region Ownership

    /// <summary>
    /// User ID of the person responsible for this deliverable
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Owner name (cached)
    /// </summary>
    [MaxLength(200)]
    public string? OwnerName { get; set; }

    /// <summary>
    /// User ID of the internal reviewer
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Reviewer name (cached)
    /// </summary>
    [MaxLength(200)]
    public string? ReviewerName { get; set; }

    /// <summary>
    /// Client contact who will approve
    /// </summary>
    public Guid? ClientApproverId { get; set; }

    /// <summary>
    /// Client approver name (cached)
    /// </summary>
    [MaxLength(200)]
    public string? ClientApproverName { get; set; }

    #endregion

    #region Review

    /// <summary>
    /// Internal review date
    /// </summary>
    public DateTime? InternalReviewDate { get; set; }

    /// <summary>
    /// Internal review comments
    /// </summary>
    public string? InternalReviewComments { get; set; }

    /// <summary>
    /// Client review comments
    /// </summary>
    public string? ClientReviewComments { get; set; }

    /// <summary>
    /// Rejection reason if rejected
    /// </summary>
    public string? RejectionReason { get; set; }

    #endregion

    #region Quality

    /// <summary>
    /// Quality check performed
    /// </summary>
    public bool QualityCheckPassed { get; set; }

    /// <summary>
    /// Quality check date
    /// </summary>
    public DateTime? QualityCheckDate { get; set; }

    /// <summary>
    /// Quality checker user ID
    /// </summary>
    public Guid? QualityCheckerId { get; set; }

    /// <summary>
    /// Quality check notes
    /// </summary>
    public string? QualityCheckNotes { get; set; }

    #endregion

    #region Transmission

    /// <summary>
    /// Method of transmission (Email, FTP, Portal, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? TransmissionMethod { get; set; }

    /// <summary>
    /// Transmission/submittal reference number
    /// </summary>
    [MaxLength(100)]
    public string? TransmittalNumber { get; set; }

    /// <summary>
    /// Recipients of the deliverable (JSON array)
    /// </summary>
    public string? RecipientsJson { get; set; }

    #endregion

    #region Dependencies

    /// <summary>
    /// Parent deliverable ID (for sub-deliverables)
    /// </summary>
    public Guid? ParentDeliverableId { get; set; }

    /// <summary>
    /// Navigation property to parent deliverable
    /// </summary>
    [ForeignKey(nameof(ParentDeliverableId))]
    [JsonIgnore]
    public virtual ProjectDeliverable? ParentDeliverable { get; set; }

    /// <summary>
    /// Child deliverables
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectDeliverable> ChildDeliverables { get; set; } = new List<ProjectDeliverable>();

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// Notes about this deliverable
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Specifications or requirements
    /// </summary>
    public string? Specifications { get; set; }

    #endregion

    #region Audit

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }
    public bool IsModifiedLocally { get; set; }

    #endregion

    #region Computed Properties

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(DeliverableNumber)
        ? $"{DeliverableNumber}: {Name}"
        : Name;

    [NotMapped]
    public string FullReference => !string.IsNullOrEmpty(RevisionNumber)
        ? $"{DeliverableNumber ?? Name} Rev {RevisionNumber}"
        : DeliverableNumber ?? Name;

    [NotMapped]
    public bool IsOverdue => PlannedDueDate.HasValue &&
                             SubmissionDate == null &&
                             PlannedDueDate.Value < DateTime.Today &&
                             Status != DeliverableStatus.Submitted &&
                             Status != DeliverableStatus.Approved &&
                             Status != DeliverableStatus.Accepted;

    [NotMapped]
    public bool IsDueSoon => PlannedDueDate.HasValue &&
                            SubmissionDate == null &&
                            PlannedDueDate.Value <= DateTime.Today.AddDays(7) &&
                            PlannedDueDate.Value >= DateTime.Today &&
                            Status != DeliverableStatus.Submitted &&
                            Status != DeliverableStatus.Approved &&
                            Status != DeliverableStatus.Accepted;

    [NotMapped]
    public int? DaysUntilDue => PlannedDueDate.HasValue
        ? (int)(PlannedDueDate.Value - DateTime.Today).TotalDays
        : null;

    [NotMapped]
    public int? DaysOverdue => IsOverdue && PlannedDueDate.HasValue
        ? (int)(DateTime.Today - PlannedDueDate.Value).TotalDays
        : null;

    [NotMapped]
    public int? ScheduleVarianceDays => PlannedDueDate.HasValue && SubmissionDate.HasValue
        ? (int)(PlannedDueDate.Value - SubmissionDate.Value).TotalDays
        : null;

    [NotMapped]
    public string StatusDisplay => Status.ToString();

    [NotMapped]
    public string TypeDisplay => Type.ToString();

    [NotMapped]
    public string FormatDisplay => Format.ToString();

    [NotMapped]
    public string PriorityDisplay => Priority.ToString();

    [NotMapped]
    public string FileSizeDisplay => FileSizeBytes.HasValue
        ? FileSizeBytes.Value < 1024 ? $"{FileSizeBytes.Value} B"
        : FileSizeBytes.Value < 1024 * 1024 ? $"{FileSizeBytes.Value / 1024.0:F1} KB"
        : FileSizeBytes.Value < 1024 * 1024 * 1024 ? $"{FileSizeBytes.Value / (1024.0 * 1024.0):F1} MB"
        : $"{FileSizeBytes.Value / (1024.0 * 1024.0 * 1024.0):F2} GB"
        : "-";

    #endregion
}
