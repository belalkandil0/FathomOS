using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service interface for managing calibration reminders and notifications.
/// </summary>
public interface ICalibrationReminderService
{
    /// <summary>
    /// Get equipment that needs calibration within the specified number of days.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead (default 30)</param>
    /// <returns>List of equipment needing calibration</returns>
    Task<List<CalibrationReminderItem>> GetUpcomingCalibrationsAsync(int daysAhead = 30);

    /// <summary>
    /// Get equipment that is past due for calibration.
    /// </summary>
    /// <returns>List of overdue equipment</returns>
    Task<List<CalibrationReminderItem>> GetOverdueCalibrationsAsync();

    /// <summary>
    /// Set the default reminder threshold in days before calibration is due.
    /// </summary>
    /// <param name="days">Number of days before due date to trigger reminders</param>
    void SetReminderThresholdDays(int days);

    /// <summary>
    /// Get the current reminder threshold in days.
    /// </summary>
    int GetReminderThresholdDays();

    /// <summary>
    /// Get all calibration reminders (both upcoming and overdue).
    /// </summary>
    /// <returns>Combined list of all calibration reminders</returns>
    Task<List<CalibrationReminderItem>> GetAllCalibrationRemindersAsync();

    /// <summary>
    /// Get calibration summary statistics.
    /// </summary>
    /// <returns>Summary of calibration status across all equipment</returns>
    Task<CalibrationSummary> GetCalibrationSummaryAsync();

    /// <summary>
    /// Get equipment by calibration status.
    /// </summary>
    /// <param name="status">The calibration status to filter by</param>
    /// <returns>List of equipment with the specified status</returns>
    Task<List<Equipment>> GetEquipmentByCalibrationStatusAsync(CalibrationStatus status);

    /// <summary>
    /// Record a calibration for equipment.
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <param name="calibrationDate">Date of calibration</param>
    /// <param name="performedBy">Person/entity who performed calibration</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="certificateNumber">Optional calibration certificate number</param>
    /// <returns>The updated equipment</returns>
    Task<Equipment> RecordCalibrationAsync(
        Guid equipmentId,
        DateTime calibrationDate,
        string? performedBy = null,
        string? notes = null,
        string? certificateNumber = null);

    /// <summary>
    /// Get calibration history for equipment.
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <returns>List of calibration history records</returns>
    Task<List<MaintenanceRecord>> GetCalibrationHistoryAsync(Guid equipmentId);
}

/// <summary>
/// Represents a calibration reminder item with equipment details.
/// </summary>
public class CalibrationReminderItem
{
    public Guid EquipmentId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string? UniqueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? LocationName { get; set; }
    public Guid? LocationId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime? LastCalibrationDate { get; set; }
    public DateTime? NextCalibrationDate { get; set; }
    public int? CalibrationIntervalDays { get; set; }
    public int DaysUntilDue { get; set; }
    public CalibrationStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public bool IsOverdue => Status == CalibrationStatus.Overdue;
    public bool IsDueSoon => Status == CalibrationStatus.DueSoon;
    public Equipment? Equipment { get; set; }
}

/// <summary>
/// Calibration status for equipment.
/// </summary>
public enum CalibrationStatus
{
    /// <summary>
    /// Equipment does not require calibration
    /// </summary>
    NotRequired,

    /// <summary>
    /// Calibration is current and not due soon
    /// </summary>
    Current,

    /// <summary>
    /// Calibration is due soon (within threshold)
    /// </summary>
    DueSoon,

    /// <summary>
    /// Calibration is overdue
    /// </summary>
    Overdue,

    /// <summary>
    /// No calibration date set for equipment that requires it
    /// </summary>
    Unknown
}

/// <summary>
/// Summary statistics for calibration status across equipment.
/// </summary>
public class CalibrationSummary
{
    /// <summary>
    /// Total equipment that requires calibration
    /// </summary>
    public int TotalRequiringCalibration { get; set; }

    /// <summary>
    /// Equipment with current calibration
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Equipment with calibration due soon
    /// </summary>
    public int DueSoonCount { get; set; }

    /// <summary>
    /// Equipment with overdue calibration
    /// </summary>
    public int OverdueCount { get; set; }

    /// <summary>
    /// Equipment with unknown calibration status
    /// </summary>
    public int UnknownCount { get; set; }

    /// <summary>
    /// Percentage of equipment with current calibration
    /// </summary>
    public double CompliancePercentage =>
        TotalRequiringCalibration > 0
            ? (double)CurrentCount / TotalRequiringCalibration * 100
            : 100;

    /// <summary>
    /// List of equipment items due soon
    /// </summary>
    public List<CalibrationReminderItem> DueSoonItems { get; set; } = new();

    /// <summary>
    /// List of overdue equipment items
    /// </summary>
    public List<CalibrationReminderItem> OverdueItems { get; set; } = new();
}
