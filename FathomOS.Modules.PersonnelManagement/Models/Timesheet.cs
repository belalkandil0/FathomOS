using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a period-based timesheet with approval workflow
/// </summary>
public class Timesheet
{
    [Key]
    public Guid TimesheetId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique timesheet number (e.g., TS-2024-001234)
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string TimesheetNumber { get; set; } = string.Empty;

    /// <summary>
    /// Personnel who owns this timesheet
    /// </summary>
    public Guid PersonnelId { get; set; }

    [ForeignKey(nameof(PersonnelId))]
    [JsonIgnore]
    public virtual Personnel? Personnel { get; set; }

    /// <summary>
    /// Vessel where work was performed (if applicable)
    /// </summary>
    public Guid? VesselId { get; set; }

    /// <summary>
    /// Vessel name (denormalized)
    /// </summary>
    [MaxLength(200)]
    public string? VesselName { get; set; }

    /// <summary>
    /// Project for this timesheet period
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Project name (denormalized)
    /// </summary>
    [MaxLength(200)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Start date of timesheet period
    /// </summary>
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// End date of timesheet period
    /// </summary>
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Current status of the timesheet
    /// </summary>
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    #region Hour Totals

    /// <summary>
    /// Total regular hours for the period
    /// </summary>
    public decimal TotalRegularHours { get; set; } = 0;

    /// <summary>
    /// Total overtime hours for the period
    /// </summary>
    public decimal TotalOvertimeHours { get; set; } = 0;

    /// <summary>
    /// Total double time hours for the period
    /// </summary>
    public decimal TotalDoubleTimeHours { get; set; } = 0;

    /// <summary>
    /// Total night shift hours for the period
    /// </summary>
    public decimal TotalNightShiftHours { get; set; } = 0;

    /// <summary>
    /// Total standby hours for the period
    /// </summary>
    public decimal TotalStandbyHours { get; set; } = 0;

    /// <summary>
    /// Total travel hours for the period
    /// </summary>
    public decimal TotalTravelHours { get; set; } = 0;

    /// <summary>
    /// Total all hours combined
    /// </summary>
    [NotMapped]
    public decimal TotalHours => TotalRegularHours + TotalOvertimeHours +
        TotalDoubleTimeHours + TotalNightShiftHours +
        TotalStandbyHours + TotalTravelHours;

    #endregion

    #region Leave Tracking

    /// <summary>
    /// Total days of leave in this period
    /// </summary>
    public decimal TotalLeaveDays { get; set; } = 0;

    /// <summary>
    /// Total sick days in this period
    /// </summary>
    public decimal TotalSickDays { get; set; } = 0;

    #endregion

    #region Submission

    /// <summary>
    /// Date timesheet was submitted
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Submission comments from employee
    /// </summary>
    public string? SubmissionComments { get; set; }

    #endregion

    #region Approval

    /// <summary>
    /// Supervisor who approved the timesheet
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Date timesheet was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Approval comments
    /// </summary>
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// Rejected by (if rejected)
    /// </summary>
    public Guid? RejectedBy { get; set; }

    /// <summary>
    /// Date timesheet was rejected
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// Rejection reason
    /// </summary>
    public string? RejectionReason { get; set; }

    #endregion

    #region Processing

    /// <summary>
    /// Date processed for payroll
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Payroll batch reference
    /// </summary>
    [MaxLength(50)]
    public string? PayrollBatchReference { get; set; }

    /// <summary>
    /// SAP document number after export
    /// </summary>
    [MaxLength(50)]
    public string? SapDocumentNumber { get; set; }

    #endregion

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    #region Audit

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }
    public bool IsModifiedLocally { get; set; }

    #endregion

    #region Navigation Properties

    [JsonIgnore]
    public virtual ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Period duration in days
    /// </summary>
    [NotMapped]
    public int PeriodDays => (int)(PeriodEndDate - PeriodStartDate).TotalDays + 1;

    /// <summary>
    /// Period display (e.g., "01 Jan - 28 Jan 2024")
    /// </summary>
    [NotMapped]
    public string PeriodDisplay => $"{PeriodStartDate:dd MMM} - {PeriodEndDate:dd MMM yyyy}";

    /// <summary>
    /// Whether timesheet can be edited
    /// </summary>
    [NotMapped]
    public bool CanEdit => Status == TimesheetStatus.Draft || Status == TimesheetStatus.Rejected;

    /// <summary>
    /// Whether timesheet can be submitted
    /// </summary>
    [NotMapped]
    public bool CanSubmit => Status == TimesheetStatus.Draft || Status == TimesheetStatus.Rejected;

    /// <summary>
    /// Whether timesheet can be approved
    /// </summary>
    [NotMapped]
    public bool CanApprove => Status == TimesheetStatus.Submitted;

    /// <summary>
    /// Status display with date
    /// </summary>
    [NotMapped]
    public string StatusDisplay
    {
        get
        {
            return Status switch
            {
                TimesheetStatus.Draft => "Draft",
                TimesheetStatus.Submitted => $"Submitted {SubmittedAt:dd MMM}",
                TimesheetStatus.Approved => $"Approved {ApprovedAt:dd MMM}",
                TimesheetStatus.Rejected => $"Rejected {RejectedAt:dd MMM}",
                TimesheetStatus.Processed => $"Processed {ProcessedAt:dd MMM}",
                _ => Status.ToString()
            };
        }
    }

    #endregion
}
