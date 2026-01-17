using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents the assignment of equipment to a project.
/// Links to Equipment entity in EquipmentInventory module via EquipmentId.
/// </summary>
public class ProjectEquipmentAssignment
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
    /// Foreign key to the equipment (references EquipmentInventory.Equipment)
    /// </summary>
    public Guid EquipmentId { get; set; }

    /// <summary>
    /// Cached equipment asset number for display without cross-module lookup
    /// </summary>
    [MaxLength(50)]
    public string? EquipmentAssetNumber { get; set; }

    /// <summary>
    /// Cached equipment name
    /// </summary>
    [MaxLength(200)]
    public string? EquipmentName { get; set; }

    /// <summary>
    /// Cached equipment serial number
    /// </summary>
    [MaxLength(100)]
    public string? EquipmentSerialNumber { get; set; }

    /// <summary>
    /// Optional link to vessel assignment if equipment is assigned to specific vessel
    /// </summary>
    public Guid? VesselAssignmentId { get; set; }

    /// <summary>
    /// Navigation property to the vessel assignment
    /// </summary>
    [ForeignKey(nameof(VesselAssignmentId))]
    [JsonIgnore]
    public virtual ProjectVesselAssignment? VesselAssignment { get; set; }

    #endregion

    #region Role

    /// <summary>
    /// Role of the equipment in this project
    /// </summary>
    public EquipmentRole Role { get; set; } = EquipmentRole.Primary;

    /// <summary>
    /// Assignment status
    /// </summary>
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Proposed;

    /// <summary>
    /// Quantity assigned (for consumables or multiples)
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit of measure for quantity
    /// </summary>
    [MaxLength(20)]
    public string UnitOfMeasure { get; set; } = "Each";

    #endregion

    #region Schedule - Planned

    /// <summary>
    /// Planned start date for equipment assignment
    /// </summary>
    public DateTime? PlannedStartDate { get; set; }

    /// <summary>
    /// Planned end date for equipment assignment
    /// </summary>
    public DateTime? PlannedEndDate { get; set; }

    /// <summary>
    /// Planned mobilization location
    /// </summary>
    [MaxLength(200)]
    public string? PlannedMobilizationLocation { get; set; }

    /// <summary>
    /// Planned demobilization location
    /// </summary>
    [MaxLength(200)]
    public string? PlannedDemobilizationLocation { get; set; }

    #endregion

    #region Schedule - Actual

    /// <summary>
    /// Actual start date for equipment assignment
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual end date for equipment assignment
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Actual mobilization location
    /// </summary>
    [MaxLength(200)]
    public string? ActualMobilizationLocation { get; set; }

    /// <summary>
    /// Actual demobilization location
    /// </summary>
    [MaxLength(200)]
    public string? ActualDemobilizationLocation { get; set; }

    #endregion

    #region Rates & Costs

    /// <summary>
    /// Daily rate for the equipment
    /// </summary>
    public decimal? DayRate { get; set; }

    /// <summary>
    /// Currency for rates
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
    /// Hours of operation (for usage tracking)
    /// </summary>
    public decimal? OperationalHours { get; set; }

    #endregion

    #region Certification

    /// <summary>
    /// Is calibration/certification required before mobilization
    /// </summary>
    public bool RequiresCertification { get; set; }

    /// <summary>
    /// Certification expiry date
    /// </summary>
    public DateTime? CertificationExpiryDate { get; set; }

    /// <summary>
    /// Is certification valid for the project duration
    /// </summary>
    [NotMapped]
    public bool IsCertificationValid => !RequiresCertification ||
        (CertificationExpiryDate.HasValue &&
         CertificationExpiryDate.Value >= (PlannedEndDate ?? DateTime.Today));

    #endregion

    #region Notes

    /// <summary>
    /// Notes about this equipment assignment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Special configuration requirements
    /// </summary>
    public string? ConfigurationNotes { get; set; }

    /// <summary>
    /// Issues or problems encountered
    /// </summary>
    public string? IssuesNotes { get; set; }

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
    public int TotalDays => OperationalDays + StandbyDays;

    [NotMapped]
    public decimal? CalculatedCost => DayRate.HasValue
        ? (DayRate.Value * TotalDays * Quantity) +
          (MobilizationCost ?? 0) +
          (DemobilizationCost ?? 0)
        : null;

    [NotMapped]
    public string RoleDisplay => Role.ToString();

    [NotMapped]
    public string StatusDisplay => Status.ToString();

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(EquipmentAssetNumber)
        ? $"{EquipmentAssetNumber} - {EquipmentName}"
        : EquipmentName ?? EquipmentId.ToString();

    #endregion
}
