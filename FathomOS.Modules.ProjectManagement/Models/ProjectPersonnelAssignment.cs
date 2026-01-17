using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents the assignment of personnel to a project.
/// Can link to User entity via UserId or store external personnel information.
/// </summary>
public class ProjectPersonnelAssignment
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
    /// User ID if internal personnel (references User entity)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Optional link to vessel assignment if personnel is assigned to specific vessel
    /// </summary>
    public Guid? VesselAssignmentId { get; set; }

    /// <summary>
    /// Navigation property to the vessel assignment
    /// </summary>
    [ForeignKey(nameof(VesselAssignmentId))]
    [JsonIgnore]
    public virtual ProjectVesselAssignment? VesselAssignment { get; set; }

    #endregion

    #region Personnel Information

    /// <summary>
    /// Personnel first name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Personnel last name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [MaxLength(30)]
    public string? Phone { get; set; }

    /// <summary>
    /// Employee ID or contractor ID
    /// </summary>
    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// Is this an internal employee or external contractor
    /// </summary>
    public bool IsInternal { get; set; } = true;

    /// <summary>
    /// Contractor company name (if external)
    /// </summary>
    [MaxLength(200)]
    public string? ContractorCompany { get; set; }

    #endregion

    #region Role

    /// <summary>
    /// Role of the personnel in this project
    /// </summary>
    public PersonnelRole Role { get; set; } = PersonnelRole.Surveyor;

    /// <summary>
    /// Specific job title for this assignment
    /// </summary>
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Assignment status
    /// </summary>
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Proposed;

    /// <summary>
    /// Is this person the lead for their role
    /// </summary>
    public bool IsLead { get; set; }

    #endregion

    #region Schedule - Planned

    /// <summary>
    /// Planned start date for personnel assignment
    /// </summary>
    public DateTime? PlannedStartDate { get; set; }

    /// <summary>
    /// Planned end date for personnel assignment
    /// </summary>
    public DateTime? PlannedEndDate { get; set; }

    /// <summary>
    /// Planned rotation (e.g., "4 weeks on / 4 weeks off")
    /// </summary>
    [MaxLength(50)]
    public string? PlannedRotation { get; set; }

    #endregion

    #region Schedule - Actual

    /// <summary>
    /// Actual start date for personnel assignment
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual end date for personnel assignment
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    #endregion

    #region Rates & Costs

    /// <summary>
    /// Daily rate for the personnel
    /// </summary>
    public decimal? DayRate { get; set; }

    /// <summary>
    /// Currency for rates
    /// </summary>
    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;

    /// <summary>
    /// Overtime rate (per hour)
    /// </summary>
    public decimal? OvertimeRate { get; set; }

    /// <summary>
    /// Mobilization cost (flights, travel, etc.)
    /// </summary>
    public decimal? MobilizationCost { get; set; }

    /// <summary>
    /// Demobilization cost
    /// </summary>
    public decimal? DemobilizationCost { get; set; }

    /// <summary>
    /// Per diem allowance
    /// </summary>
    public decimal? PerDiemRate { get; set; }

    /// <summary>
    /// Estimated total cost for this assignment
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Actual cost to date
    /// </summary>
    public decimal? ActualCost { get; set; }

    #endregion

    #region Time Tracking

    /// <summary>
    /// Number of days worked
    /// </summary>
    public int DaysWorked { get; set; }

    /// <summary>
    /// Number of days on standby
    /// </summary>
    public int StandbyDays { get; set; }

    /// <summary>
    /// Number of travel days
    /// </summary>
    public int TravelDays { get; set; }

    /// <summary>
    /// Number of sick days
    /// </summary>
    public int SickDays { get; set; }

    /// <summary>
    /// Overtime hours worked
    /// </summary>
    public decimal OvertimeHours { get; set; }

    #endregion

    #region Certifications

    /// <summary>
    /// Required certifications for this role
    /// </summary>
    public string? RequiredCertifications { get; set; }

    /// <summary>
    /// Certifications held by this person (JSON array)
    /// </summary>
    public string? CertificationsJson { get; set; }

    /// <summary>
    /// Medical certificate expiry date
    /// </summary>
    public DateTime? MedicalExpiryDate { get; set; }

    /// <summary>
    /// Offshore survival certificate expiry date
    /// </summary>
    public DateTime? SurvivalCertExpiryDate { get; set; }

    /// <summary>
    /// Are all certifications valid
    /// </summary>
    public bool CertificationsValid { get; set; } = true;

    #endregion

    #region Emergency Contact

    /// <summary>
    /// Emergency contact name
    /// </summary>
    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    /// <summary>
    /// Emergency contact phone
    /// </summary>
    [MaxLength(30)]
    public string? EmergencyContactPhone { get; set; }

    /// <summary>
    /// Emergency contact relationship
    /// </summary>
    [MaxLength(50)]
    public string? EmergencyContactRelation { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// Notes about this personnel assignment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Performance notes
    /// </summary>
    public string? PerformanceNotes { get; set; }

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
    public string FullName => $"{FirstName} {LastName}".Trim();

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
    public int TotalDays => DaysWorked + StandbyDays + TravelDays + SickDays;

    [NotMapped]
    public decimal? CalculatedCost => DayRate.HasValue
        ? (DayRate.Value * DaysWorked) +
          (DayRate.Value * StandbyDays) +
          (OvertimeRate ?? 0) * OvertimeHours +
          (PerDiemRate ?? 0) * TotalDays +
          (MobilizationCost ?? 0) +
          (DemobilizationCost ?? 0)
        : null;

    [NotMapped]
    public string RoleDisplay => Role.ToString();

    [NotMapped]
    public string StatusDisplay => Status.ToString();

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(JobTitle)
        ? $"{FullName} ({JobTitle})"
        : FullName;

    [NotMapped]
    public bool IsMedicalExpiring => MedicalExpiryDate.HasValue &&
        MedicalExpiryDate.Value <= DateTime.Today.AddDays(30);

    [NotMapped]
    public bool IsSurvivalCertExpiring => SurvivalCertExpiryDate.HasValue &&
        SurvivalCertExpiryDate.Value <= DateTime.Today.AddDays(30);

    #endregion
}
