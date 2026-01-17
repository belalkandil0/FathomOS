using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a daily time entry within a timesheet
/// </summary>
public class TimesheetEntry
{
    [Key]
    public Guid TimesheetEntryId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Parent timesheet
    /// </summary>
    public Guid TimesheetId { get; set; }

    [ForeignKey(nameof(TimesheetId))]
    [JsonIgnore]
    public virtual Timesheet? Timesheet { get; set; }

    /// <summary>
    /// Date of this entry
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// Type of time entry
    /// </summary>
    public TimeEntryType EntryType { get; set; } = TimeEntryType.Regular;

    /// <summary>
    /// Work start time
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Work end time
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Break duration in hours
    /// </summary>
    public decimal BreakHours { get; set; } = 0;

    /// <summary>
    /// Total hours worked (excluding breaks)
    /// </summary>
    public decimal TotalHours { get; set; } = 0;

    /// <summary>
    /// Regular hours (up to standard daily hours)
    /// </summary>
    public decimal RegularHours { get; set; } = 0;

    /// <summary>
    /// Overtime hours
    /// </summary>
    public decimal OvertimeHours { get; set; } = 0;

    /// <summary>
    /// Double time hours (e.g., holidays)
    /// </summary>
    public decimal DoubleTimeHours { get; set; } = 0;

    /// <summary>
    /// Night shift premium hours
    /// </summary>
    public decimal NightShiftHours { get; set; } = 0;

    /// <summary>
    /// Standby hours
    /// </summary>
    public decimal StandbyHours { get; set; } = 0;

    /// <summary>
    /// Travel hours
    /// </summary>
    public decimal TravelHours { get; set; } = 0;

    /// <summary>
    /// Whether this is a leave day
    /// </summary>
    public bool IsLeave { get; set; } = false;

    /// <summary>
    /// Type of leave if applicable
    /// </summary>
    public LeaveType? LeaveType { get; set; }

    /// <summary>
    /// Leave hours (for partial leave days)
    /// </summary>
    public decimal LeaveHours { get; set; } = 0;

    /// <summary>
    /// Whether this is a sick day
    /// </summary>
    public bool IsSickDay { get; set; } = false;

    /// <summary>
    /// Whether this is a public holiday
    /// </summary>
    public bool IsPublicHoliday { get; set; } = false;

    /// <summary>
    /// Holiday name if applicable
    /// </summary>
    [MaxLength(100)]
    public string? HolidayName { get; set; }

    /// <summary>
    /// Whether this is a rest day (as per rotation)
    /// </summary>
    public bool IsRestDay { get; set; } = false;

    /// <summary>
    /// Work location for this day
    /// </summary>
    [MaxLength(200)]
    public string? WorkLocation { get; set; }

    /// <summary>
    /// Activity/task description
    /// </summary>
    [MaxLength(500)]
    public string? ActivityDescription { get; set; }

    /// <summary>
    /// Cost center/work order for billing
    /// </summary>
    [MaxLength(50)]
    public string? CostCenter { get; set; }

    /// <summary>
    /// Work order number
    /// </summary>
    [MaxLength(50)]
    public string? WorkOrderNumber { get; set; }

    /// <summary>
    /// Additional notes for this entry
    /// </summary>
    public string? Notes { get; set; }

    #region Audit

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Day of week display
    /// </summary>
    [NotMapped]
    public string DayOfWeekDisplay => EntryDate.ToString("ddd");

    /// <summary>
    /// Date display
    /// </summary>
    [NotMapped]
    public string DateDisplay => EntryDate.ToString("dd MMM");

    /// <summary>
    /// Full date display
    /// </summary>
    [NotMapped]
    public string FullDateDisplay => EntryDate.ToString("ddd, dd MMM yyyy");

    /// <summary>
    /// Time range display
    /// </summary>
    [NotMapped]
    public string TimeRangeDisplay
    {
        get
        {
            if (!StartTime.HasValue || !EndTime.HasValue) return "-";
            return $"{StartTime.Value:hh\\:mm} - {EndTime.Value:hh\\:mm}";
        }
    }

    /// <summary>
    /// All hours combined
    /// </summary>
    [NotMapped]
    public decimal AllHours => RegularHours + OvertimeHours + DoubleTimeHours +
        NightShiftHours + StandbyHours + TravelHours;

    /// <summary>
    /// Whether this is a weekend
    /// </summary>
    [NotMapped]
    public bool IsWeekend => EntryDate.DayOfWeek == DayOfWeek.Saturday ||
        EntryDate.DayOfWeek == DayOfWeek.Sunday;

    /// <summary>
    /// Entry type display
    /// </summary>
    [NotMapped]
    public string EntryTypeDisplay
    {
        get
        {
            if (IsLeave && LeaveType.HasValue) return LeaveType.Value.ToString();
            if (IsSickDay) return "Sick";
            if (IsPublicHoliday) return "Holiday";
            if (IsRestDay) return "Rest Day";
            return EntryType.ToString();
        }
    }

    #endregion
}
