using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a personnel assignment to a vessel with sign-on/sign-off tracking
/// </summary>
public class VesselAssignment
{
    [Key]
    public Guid VesselAssignmentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Personnel assigned to the vessel
    /// </summary>
    public Guid PersonnelId { get; set; }

    [ForeignKey(nameof(PersonnelId))]
    [JsonIgnore]
    public virtual Personnel? Personnel { get; set; }

    /// <summary>
    /// Vessel ID (references shared Vessel entity)
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Vessel name (denormalized for quick display)
    /// </summary>
    [MaxLength(200)]
    public string? VesselName { get; set; }

    /// <summary>
    /// Project ID if assignment is project-specific
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Project name (denormalized for quick display)
    /// </summary>
    [MaxLength(200)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Position held during this assignment
    /// </summary>
    public Guid? PositionId { get; set; }

    [ForeignKey(nameof(PositionId))]
    [JsonIgnore]
    public virtual Position? Position { get; set; }

    /// <summary>
    /// Assignment status
    /// </summary>
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Scheduled;

    /// <summary>
    /// Scheduled start date of assignment
    /// </summary>
    public DateTime ScheduledStartDate { get; set; }

    /// <summary>
    /// Scheduled end date of assignment
    /// </summary>
    public DateTime ScheduledEndDate { get; set; }

    /// <summary>
    /// Actual sign-on date/time
    /// </summary>
    public DateTime? SignOnDateTime { get; set; }

    /// <summary>
    /// Location where sign-on occurred
    /// </summary>
    [MaxLength(200)]
    public string? SignOnLocation { get; set; }

    /// <summary>
    /// Port where sign-on occurred
    /// </summary>
    [MaxLength(100)]
    public string? SignOnPort { get; set; }

    /// <summary>
    /// Actual sign-off date/time
    /// </summary>
    public DateTime? SignOffDateTime { get; set; }

    /// <summary>
    /// Location where sign-off occurred
    /// </summary>
    [MaxLength(200)]
    public string? SignOffLocation { get; set; }

    /// <summary>
    /// Port where sign-off occurred
    /// </summary>
    [MaxLength(100)]
    public string? SignOffPort { get; set; }

    /// <summary>
    /// Reason for sign-off (rotation, project end, medical, etc.)
    /// </summary>
    [MaxLength(200)]
    public string? SignOffReason { get; set; }

    /// <summary>
    /// Travel departure date
    /// </summary>
    public DateTime? TravelDepartureDate { get; set; }

    /// <summary>
    /// Travel departure location
    /// </summary>
    [MaxLength(200)]
    public string? TravelDepartureFrom { get; set; }

    /// <summary>
    /// Travel arrival date
    /// </summary>
    public DateTime? TravelArrivalDate { get; set; }

    /// <summary>
    /// Flight booking reference
    /// </summary>
    [MaxLength(100)]
    public string? FlightBookingReference { get; set; }

    /// <summary>
    /// Hotel accommodation details
    /// </summary>
    public string? AccommodationDetails { get; set; }

    /// <summary>
    /// Personnel replacing this person (handover)
    /// </summary>
    public Guid? ReplacementPersonnelId { get; set; }

    /// <summary>
    /// Personnel being replaced
    /// </summary>
    public Guid? ReplacingPersonnelId { get; set; }

    /// <summary>
    /// Handover completed flag
    /// </summary>
    public bool HandoverCompleted { get; set; } = false;

    /// <summary>
    /// Handover notes
    /// </summary>
    public string? HandoverNotes { get; set; }

    /// <summary>
    /// Additional notes for this assignment
    /// </summary>
    public string? Notes { get; set; }

    #region Audit

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Actual days on vessel
    /// </summary>
    [NotMapped]
    public int? ActualDaysOnVessel
    {
        get
        {
            if (!SignOnDateTime.HasValue) return null;
            var endDate = SignOffDateTime ?? DateTime.UtcNow;
            return (int)(endDate - SignOnDateTime.Value).TotalDays;
        }
    }

    /// <summary>
    /// Scheduled duration in days
    /// </summary>
    [NotMapped]
    public int ScheduledDuration => (int)(ScheduledEndDate - ScheduledStartDate).TotalDays;

    /// <summary>
    /// Whether currently on vessel (signed on but not signed off)
    /// </summary>
    [NotMapped]
    public bool IsCurrentlyOnVessel => SignOnDateTime.HasValue && !SignOffDateTime.HasValue;

    /// <summary>
    /// Whether assignment is upcoming (scheduled but not yet signed on)
    /// </summary>
    [NotMapped]
    public bool IsUpcoming => Status == AssignmentStatus.Scheduled && !SignOnDateTime.HasValue;

    /// <summary>
    /// Whether assignment is overdue for sign-on
    /// </summary>
    [NotMapped]
    public bool IsOverdueForSignOn => Status == AssignmentStatus.Scheduled &&
        !SignOnDateTime.HasValue &&
        ScheduledStartDate < DateTime.Today;

    /// <summary>
    /// Status display text
    /// </summary>
    [NotMapped]
    public string StatusDisplay
    {
        get
        {
            if (IsCurrentlyOnVessel) return $"On Vessel ({ActualDaysOnVessel} days)";
            if (IsUpcoming) return $"Scheduled ({(ScheduledStartDate - DateTime.Today).Days} days)";
            return Status.ToString();
        }
    }

    #endregion
}
