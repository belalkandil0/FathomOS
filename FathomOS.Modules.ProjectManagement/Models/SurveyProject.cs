using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents a survey project - the core entity of the Project Management module.
/// </summary>
public class SurveyProject
{
    #region Identification

    [Key]
    public Guid ProjectId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique project number (e.g., PRJ-2024-00001)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>
    /// Project name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Short code for the project (e.g., ARAMCO-SURVEY-001)
    /// </summary>
    [MaxLength(50)]
    public string? ProjectCode { get; set; }

    /// <summary>
    /// SAP project/WBS number for integration
    /// </summary>
    [MaxLength(50)]
    public string? SapProjectNumber { get; set; }

    /// <summary>
    /// Client's reference number for the project
    /// </summary>
    [MaxLength(100)]
    public string? ClientReference { get; set; }

    /// <summary>
    /// Contract number
    /// </summary>
    [MaxLength(100)]
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Purchase order number
    /// </summary>
    [MaxLength(100)]
    public string? PurchaseOrderNumber { get; set; }

    #endregion

    #region Client

    /// <summary>
    /// Foreign key to the client
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Navigation property to the client
    /// </summary>
    [ForeignKey(nameof(ClientId))]
    [JsonIgnore]
    public virtual Client? Client { get; set; }

    /// <summary>
    /// Primary client contact for this project
    /// </summary>
    public Guid? PrimaryContactId { get; set; }

    /// <summary>
    /// Navigation property to the primary contact
    /// </summary>
    [ForeignKey(nameof(PrimaryContactId))]
    [JsonIgnore]
    public virtual ClientContact? PrimaryContact { get; set; }

    #endregion

    #region Classification

    /// <summary>
    /// Type of project (Hydrographic, Geophysical, etc.)
    /// </summary>
    public ProjectType ProjectType { get; set; } = ProjectType.Other;

    /// <summary>
    /// Current project status
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    /// <summary>
    /// Current project phase
    /// </summary>
    public ProjectPhase Phase { get; set; } = ProjectPhase.Initiation;

    /// <summary>
    /// Priority level of the project
    /// </summary>
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    /// <summary>
    /// Billing type for the project
    /// </summary>
    public BillingType BillingType { get; set; } = BillingType.DayRate;

    #endregion

    #region Description

    /// <summary>
    /// Detailed project description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Scope of work summary
    /// </summary>
    public string? ScopeOfWork { get; set; }

    /// <summary>
    /// Project objectives
    /// </summary>
    public string? Objectives { get; set; }

    #endregion

    #region Location

    /// <summary>
    /// Geographic region of the project
    /// </summary>
    [MaxLength(100)]
    public string? Region { get; set; }

    /// <summary>
    /// Country where project is located
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// Specific location/area name
    /// </summary>
    [MaxLength(200)]
    public string? LocationName { get; set; }

    /// <summary>
    /// Water depth range (e.g., "50-200m")
    /// </summary>
    [MaxLength(50)]
    public string? WaterDepthRange { get; set; }

    /// <summary>
    /// Survey area in square kilometers
    /// </summary>
    public decimal? SurveyAreaKm2 { get; set; }

    /// <summary>
    /// Line kilometers to survey
    /// </summary>
    public decimal? LineKilometers { get; set; }

    /// <summary>
    /// Bounding box or area coordinates as JSON
    /// </summary>
    public string? AreaCoordinatesJson { get; set; }

    #endregion

    #region Schedule - Planned

    /// <summary>
    /// Planned project start date
    /// </summary>
    public DateTime? PlannedStartDate { get; set; }

    /// <summary>
    /// Planned project end date
    /// </summary>
    public DateTime? PlannedEndDate { get; set; }

    /// <summary>
    /// Planned mobilization date
    /// </summary>
    public DateTime? PlannedMobilizationDate { get; set; }

    /// <summary>
    /// Planned demobilization date
    /// </summary>
    public DateTime? PlannedDemobilizationDate { get; set; }

    /// <summary>
    /// Estimated duration in days
    /// </summary>
    public int? EstimatedDurationDays { get; set; }

    #endregion

    #region Schedule - Actual

    /// <summary>
    /// Actual project start date
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual project end date
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Actual mobilization date
    /// </summary>
    public DateTime? ActualMobilizationDate { get; set; }

    /// <summary>
    /// Actual demobilization date
    /// </summary>
    public DateTime? ActualDemobilizationDate { get; set; }

    #endregion

    #region Budget & Financials

    /// <summary>
    /// Total project budget
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Currency for the budget
    /// </summary>
    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    /// <summary>
    /// Contract value
    /// </summary>
    public decimal? ContractValue { get; set; }

    /// <summary>
    /// Estimated cost
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Actual cost to date
    /// </summary>
    public decimal? ActualCost { get; set; }

    /// <summary>
    /// Amount invoiced to date
    /// </summary>
    public decimal? InvoicedAmount { get; set; }

    /// <summary>
    /// Amount paid to date
    /// </summary>
    public decimal? PaidAmount { get; set; }

    /// <summary>
    /// Daily vessel rate
    /// </summary>
    public decimal? VesselDayRate { get; set; }

    /// <summary>
    /// Daily equipment rate
    /// </summary>
    public decimal? EquipmentDayRate { get; set; }

    /// <summary>
    /// Daily personnel rate
    /// </summary>
    public decimal? PersonnelDayRate { get; set; }

    #endregion

    #region Progress

    /// <summary>
    /// Overall completion percentage (0-100)
    /// </summary>
    public decimal PercentComplete { get; set; }

    /// <summary>
    /// Days on standby
    /// </summary>
    public int StandbyDays { get; set; }

    /// <summary>
    /// Days with weather downtime
    /// </summary>
    public int WeatherDowntimeDays { get; set; }

    /// <summary>
    /// Operational days
    /// </summary>
    public int OperationalDays { get; set; }

    #endregion

    #region Team

    /// <summary>
    /// Internal Project Manager user ID
    /// </summary>
    public Guid? ProjectManagerId { get; set; }

    /// <summary>
    /// Party Chief / Offshore Manager user ID
    /// </summary>
    public Guid? PartyChiefId { get; set; }

    /// <summary>
    /// Technical Lead user ID
    /// </summary>
    public Guid? TechnicalLeadId { get; set; }

    /// <summary>
    /// Commercial Manager user ID
    /// </summary>
    public Guid? CommercialManagerId { get; set; }

    #endregion

    #region Risk & HSE

    /// <summary>
    /// Risk level (Low, Medium, High)
    /// </summary>
    [MaxLength(20)]
    public string? RiskLevel { get; set; }

    /// <summary>
    /// Risk assessment notes
    /// </summary>
    public string? RiskAssessmentNotes { get; set; }

    /// <summary>
    /// HSE requirements
    /// </summary>
    public string? HseRequirements { get; set; }

    /// <summary>
    /// Special permits required
    /// </summary>
    public string? PermitsRequired { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// General project notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Internal notes (not visible to client)
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Lessons learned
    /// </summary>
    public string? LessonsLearned { get; set; }

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

    #region Soft Delete

    /// <summary>
    /// Indicates whether the project has been soft-deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when the project was soft-deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID who soft-deleted the project
    /// </summary>
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Vessels assigned to this project
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectVesselAssignment> VesselAssignments { get; set; } = new List<ProjectVesselAssignment>();

    /// <summary>
    /// Equipment assigned to this project
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectEquipmentAssignment> EquipmentAssignments { get; set; } = new List<ProjectEquipmentAssignment>();

    /// <summary>
    /// Personnel assigned to this project
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectPersonnelAssignment> PersonnelAssignments { get; set; } = new List<ProjectPersonnelAssignment>();

    /// <summary>
    /// Project milestones
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();

    /// <summary>
    /// Project deliverables
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ProjectDeliverable> Deliverables { get; set; } = new List<ProjectDeliverable>();

    #endregion

    #region Computed Properties

    [NotMapped]
    public string DisplayName => $"{ProjectNumber} - {ProjectName}";

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
    public bool IsOverdue => PlannedEndDate.HasValue &&
                             ActualEndDate == null &&
                             PlannedEndDate.Value < DateTime.Today &&
                             Status != ProjectStatus.Completed &&
                             Status != ProjectStatus.Cancelled &&
                             Status != ProjectStatus.Closed;

    [NotMapped]
    public bool IsOverBudget => Budget.HasValue &&
                                ActualCost.HasValue &&
                                ActualCost.Value > Budget.Value;

    [NotMapped]
    public decimal? BudgetVariance => Budget.HasValue && ActualCost.HasValue
        ? Budget.Value - ActualCost.Value
        : null;

    [NotMapped]
    public decimal? BudgetVariancePercent => Budget.HasValue && Budget.Value != 0 && ActualCost.HasValue
        ? ((Budget.Value - ActualCost.Value) / Budget.Value) * 100
        : null;

    [NotMapped]
    public int? ScheduleVarianceDays => PlannedEndDate.HasValue && ActualEndDate.HasValue
        ? (int)(PlannedEndDate.Value - ActualEndDate.Value).TotalDays
        : PlannedEndDate.HasValue && Status == ProjectStatus.Active
            ? (int)(PlannedEndDate.Value - DateTime.Today).TotalDays
            : null;

    [NotMapped]
    public string StatusDisplay => Status.ToString();

    [NotMapped]
    public string PhaseDisplay => Phase.ToString();

    /// <summary>
    /// Indicates whether the project can be restored (is soft-deleted)
    /// </summary>
    [NotMapped]
    public bool CanBeRestored => IsDeleted;

    /// <summary>
    /// Number of days since the project was deleted
    /// </summary>
    [NotMapped]
    public int? DaysSinceDeleted => DeletedAt.HasValue
        ? (int)(DateTime.UtcNow - DeletedAt.Value).TotalDays
        : null;

    #endregion
}
