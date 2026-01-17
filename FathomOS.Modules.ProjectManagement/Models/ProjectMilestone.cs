using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents a milestone within a project.
/// </summary>
public class ProjectMilestone
{
    #region Identification

    [Key]
    public Guid MilestoneId { get; set; } = Guid.NewGuid();

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
    /// Milestone number within the project (e.g., M1, M2)
    /// </summary>
    [MaxLength(20)]
    public string? MilestoneNumber { get; set; }

    #endregion

    #region Description

    /// <summary>
    /// Milestone name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the milestone
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of milestone
    /// </summary>
    public MilestoneType Type { get; set; } = MilestoneType.Custom;

    /// <summary>
    /// Priority level
    /// </summary>
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    #endregion

    #region Status

    /// <summary>
    /// Current status of the milestone
    /// </summary>
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

    /// <summary>
    /// Completion percentage (0-100)
    /// </summary>
    public decimal PercentComplete { get; set; }

    #endregion

    #region Schedule

    /// <summary>
    /// Planned date for the milestone
    /// </summary>
    public DateTime? PlannedDate { get; set; }

    /// <summary>
    /// Forecasted date based on current progress
    /// </summary>
    public DateTime? ForecastDate { get; set; }

    /// <summary>
    /// Actual completion date
    /// </summary>
    public DateTime? ActualDate { get; set; }

    /// <summary>
    /// Original baseline date (before any changes)
    /// </summary>
    public DateTime? BaselineDate { get; set; }

    /// <summary>
    /// Reminder days before due date
    /// </summary>
    public int? ReminderDaysBefore { get; set; }

    #endregion

    #region Dependencies

    /// <summary>
    /// Parent milestone ID (for sub-milestones)
    /// </summary>
    public Guid? ParentMilestoneId { get; set; }

    /// <summary>
    /// Navigation property to parent milestone
    /// </summary>
    [ForeignKey(nameof(ParentMilestoneId))]
    [JsonIgnore]
    public virtual ProjectMilestone? ParentMilestone { get; set; }

    /// <summary>
    /// Child milestones
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectMilestone> ChildMilestones { get; set; } = new List<ProjectMilestone>();

    /// <summary>
    /// Predecessor milestone IDs (JSON array of Guids)
    /// </summary>
    public string? PredecessorIdsJson { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    #endregion

    #region Financial

    /// <summary>
    /// Is this a payment milestone
    /// </summary>
    public bool IsPaymentMilestone { get; set; }

    /// <summary>
    /// Payment amount if payment milestone
    /// </summary>
    public decimal? PaymentAmount { get; set; }

    /// <summary>
    /// Currency for payment
    /// </summary>
    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    /// <summary>
    /// Payment percentage of contract value
    /// </summary>
    public decimal? PaymentPercentage { get; set; }

    /// <summary>
    /// Invoice number if invoiced
    /// </summary>
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Invoice date
    /// </summary>
    public DateTime? InvoiceDate { get; set; }

    /// <summary>
    /// Has this payment milestone been paid
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    #endregion

    #region Ownership

    /// <summary>
    /// User ID of the person responsible for this milestone
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Owner name (cached)
    /// </summary>
    [MaxLength(200)]
    public string? OwnerName { get; set; }

    /// <summary>
    /// User IDs of reviewers (JSON array)
    /// </summary>
    public string? ReviewerIdsJson { get; set; }

    #endregion

    #region Deliverables

    /// <summary>
    /// Deliverables linked to this milestone
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectDeliverable> Deliverables { get; set; } = new List<ProjectDeliverable>();

    /// <summary>
    /// Number of deliverables required for this milestone
    /// </summary>
    public int RequiredDeliverableCount { get; set; }

    /// <summary>
    /// Number of deliverables completed
    /// </summary>
    public int CompletedDeliverableCount { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// Notes about this milestone
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Acceptance criteria
    /// </summary>
    public string? AcceptanceCriteria { get; set; }

    /// <summary>
    /// Reason for delay if delayed
    /// </summary>
    public string? DelayReason { get; set; }

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
    public string DisplayName => !string.IsNullOrEmpty(MilestoneNumber)
        ? $"{MilestoneNumber}: {Name}"
        : Name;

    [NotMapped]
    public bool IsOverdue => PlannedDate.HasValue &&
                             ActualDate == null &&
                             PlannedDate.Value < DateTime.Today &&
                             Status != MilestoneStatus.Completed &&
                             Status != MilestoneStatus.Cancelled;

    [NotMapped]
    public bool IsDueSoon => PlannedDate.HasValue &&
                            ActualDate == null &&
                            PlannedDate.Value <= DateTime.Today.AddDays(7) &&
                            PlannedDate.Value >= DateTime.Today &&
                            Status != MilestoneStatus.Completed &&
                            Status != MilestoneStatus.Cancelled;

    [NotMapped]
    public int? DaysUntilDue => PlannedDate.HasValue
        ? (int)(PlannedDate.Value - DateTime.Today).TotalDays
        : null;

    [NotMapped]
    public int? DaysOverdue => IsOverdue && PlannedDate.HasValue
        ? (int)(DateTime.Today - PlannedDate.Value).TotalDays
        : null;

    [NotMapped]
    public int? ScheduleVarianceDays => PlannedDate.HasValue && ActualDate.HasValue
        ? (int)(PlannedDate.Value - ActualDate.Value).TotalDays
        : null;

    [NotMapped]
    public string StatusDisplay => Status.ToString();

    [NotMapped]
    public string TypeDisplay => Type.ToString();

    [NotMapped]
    public string PriorityDisplay => Priority.ToString();

    [NotMapped]
    public decimal DeliverableProgress => RequiredDeliverableCount > 0
        ? (decimal)CompletedDeliverableCount / RequiredDeliverableCount * 100
        : 0;

    #endregion
}
