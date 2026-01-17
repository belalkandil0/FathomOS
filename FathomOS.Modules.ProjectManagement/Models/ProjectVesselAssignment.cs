using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents the assignment of a vessel to a project.
/// Links to Vessel entity in EquipmentInventory module via VesselId.
/// </summary>
public class ProjectVesselAssignment
{
    #region Identification

    [Key]
    public Guid AssignmentId { get; set; } = Guid.NewGuid();

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
    /// Foreign key to the vessel (references EquipmentInventory.Vessel)
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Cached vessel name for display without cross-module lookup
    /// </summary>
    [MaxLength(200)]
    public string? VesselName { get; set; }

    /// <summary>
    /// Cached vessel IMO number
    /// </summary>
    [MaxLength(20)]
    public string? VesselImoNumber { get; set; }

    #endregion

    #region Role

    /// <summary>
    /// Role of the vessel in this project
    /// </summary>
    public VesselRole Role { get; set; } = VesselRole.Primary;

    /// <summary>
    /// Assignment status
    /// </summary>
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Proposed;

    #endregion

    #region Schedule - Planned

    /// <summary>
    /// Planned start date for vessel assignment
    /// </summary>
    public DateTime? PlannedStartDate { get; set; }

    /// <summary>
    /// Planned end date for vessel assignment
    /// </summary>
    public DateTime? PlannedEndDate { get; set; }

    /// <summary>
    /// Planned mobilization port
    /// </summary>
    [MaxLength(100)]
    public string? PlannedMobilizationPort { get; set; }

    /// <summary>
    /// Planned demobilization port
    /// </summary>
    [MaxLength(100)]
    public string? PlannedDemobilizationPort { get; set; }

    #endregion

    #region Schedule - Actual

    /// <summary>
    /// Actual start date for vessel assignment
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual end date for vessel assignment
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Actual mobilization port
    /// </summary>
    [MaxLength(100)]
    public string? ActualMobilizationPort { get; set; }

    /// <summary>
    /// Actual demobilization port
    /// </summary>
    [MaxLength(100)]
    public string? ActualDemobilizationPort { get; set; }

    #endregion

    #region Rates & Costs

    /// <summary>
    /// Daily rate for the vessel
    /// </summary>
    public decimal? DayRate { get; set; }

    /// <summary>
    /// Currency for the day rate
    /// </summary>
    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    /// <summary>
    /// Mobilization cost
    /// </summary>
    public decimal? MobilizationCost { get; set; }

    /// <summary>
    /// Demobilization cost
    /// </summary>
    public decimal? DemobilizationCost { get; set; }

    /// <summary>
    /// Standby rate (if different from day rate)
    /// </summary>
    public decimal? StandbyRate { get; set; }

    /// <summary>
    /// Estimated total cost for this assignment
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Actual cost to date
    /// </summary>
    public decimal? ActualCost { get; set; }

    #endregion

    #region Operations

    /// <summary>
    /// Number of operational days
    /// </summary>
    public int OperationalDays { get; set; }

    /// <summary>
    /// Number of standby days
    /// </summary>
    public int StandbyDays { get; set; }

    /// <summary>
    /// Number of weather downtime days
    /// </summary>
    public int WeatherDowntimeDays { get; set; }

    /// <summary>
    /// Number of transit days
    /// </summary>
    public int TransitDays { get; set; }

    /// <summary>
    /// Number of port days
    /// </summary>
    public int PortDays { get; set; }

    #endregion

    #region Crew

    /// <summary>
    /// Minimum crew complement required
    /// </summary>
    public int? MinCrewCount { get; set; }

    /// <summary>
    /// Maximum personnel on board (POB)
    /// </summary>
    public int? MaxPob { get; set; }

    /// <summary>
    /// Number of survey berths available
    /// </summary>
    public int? SurveyBerths { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// Notes about this vessel assignment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Special requirements for this vessel on this project
    /// </summary>
    public string? SpecialRequirements { get; set; }

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
    public int? PlannedDurationDays => PlannedStartDate.HasValue && PlannedEndDate.HasValue
        ? (int)(PlannedEndDate.Value - PlannedStartDate.Value).TotalDays
        : null;

    [NotMapped]
    public int? ActualDurationDays => ActualStartDate.HasValue && ActualEndDate.HasValue
        ? (int)(ActualEndDate.Value - ActualStartDate.Value).TotalDays
        : ActualStartDate.HasValue
            ? (int)(DateTime.Today - ActualStartDate.Value).TotalDays
            : null;

    [NotMapped]
    public int TotalDays => OperationalDays + StandbyDays + WeatherDowntimeDays + TransitDays + PortDays;

    [NotMapped]
    public decimal? CalculatedCost => DayRate.HasValue
        ? (DayRate.Value * OperationalDays) +
          (StandbyRate ?? DayRate.Value) * StandbyDays +
          (MobilizationCost ?? 0) +
          (DemobilizationCost ?? 0)
        : null;

    [NotMapped]
    public string RoleDisplay => Role.ToString();

    [NotMapped]
    public string StatusDisplay => Status.ToString();

    #endregion
}
