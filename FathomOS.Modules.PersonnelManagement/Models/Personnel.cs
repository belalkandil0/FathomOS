using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a personnel/employee record
/// </summary>
public class Personnel
{
    #region Identification

    [Key]
    public Guid PersonnelId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique employee number (e.g., EMP-001234)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// SAP personnel number for integration
    /// </summary>
    [MaxLength(20)]
    public string? SapNumber { get; set; }

    /// <summary>
    /// Badge/access card number
    /// </summary>
    [MaxLength(50)]
    public string? BadgeNumber { get; set; }

    #endregion

    #region Personal Information

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred name/nickname
    /// </summary>
    [MaxLength(100)]
    public string? PreferredName { get; set; }

    /// <summary>
    /// Full name for display
    /// </summary>
    [NotMapped]
    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    /// <summary>
    /// Display name (preferred name or first name)
    /// </summary>
    [NotMapped]
    public string DisplayName => PreferredName ?? FirstName;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(50)]
    public string? Nationality { get; set; }

    /// <summary>
    /// Passport number
    /// </summary>
    [MaxLength(50)]
    public string? PassportNumber { get; set; }

    public DateTime? PassportExpiryDate { get; set; }

    /// <summary>
    /// Seafarer's identity document number
    /// </summary>
    [MaxLength(50)]
    public string? SeafarerBookNumber { get; set; }

    public DateTime? SeafarerBookExpiryDate { get; set; }

    #endregion

    #region Contact Information

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? PersonalEmail { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(30)]
    public string? MobileNumber { get; set; }

    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    #endregion

    #region Emergency Contact

    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactRelation { get; set; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(200)]
    public string? EmergencyContactEmail { get; set; }

    #endregion

    #region Employment

    public Guid? PositionId { get; set; }

    [ForeignKey(nameof(PositionId))]
    [JsonIgnore]
    public virtual Position? Position { get; set; }

    /// <summary>
    /// Department (denormalized for quick filtering)
    /// </summary>
    public Department Department { get; set; } = Department.Operations;

    /// <summary>
    /// Supervisor's personnel ID
    /// </summary>
    public Guid? SupervisorId { get; set; }

    [ForeignKey(nameof(SupervisorId))]
    [JsonIgnore]
    public virtual Personnel? Supervisor { get; set; }

    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// Termination reason if applicable
    /// </summary>
    [MaxLength(500)]
    public string? TerminationReason { get; set; }

    /// <summary>
    /// Date of last promotion
    /// </summary>
    public DateTime? LastPromotionDate { get; set; }

    #endregion

    #region Rotation

    public Guid? RotationPatternId { get; set; }

    [ForeignKey(nameof(RotationPatternId))]
    [JsonIgnore]
    public virtual RotationPattern? RotationPattern { get; set; }

    /// <summary>
    /// Home base location
    /// </summary>
    [MaxLength(100)]
    public string? HomeBase { get; set; }

    /// <summary>
    /// Preferred airport for travel
    /// </summary>
    [MaxLength(100)]
    public string? PreferredAirport { get; set; }

    #endregion

    #region Current Assignment

    /// <summary>
    /// Current vessel assignment (if offshore)
    /// </summary>
    public Guid? CurrentVesselId { get; set; }

    /// <summary>
    /// Current project assignment
    /// </summary>
    public Guid? CurrentProjectId { get; set; }

    /// <summary>
    /// Whether currently offshore
    /// </summary>
    public bool IsOffshore { get; set; } = false;

    /// <summary>
    /// Current rotation start date
    /// </summary>
    public DateTime? CurrentRotationStartDate { get; set; }

    /// <summary>
    /// Expected end of current rotation
    /// </summary>
    public DateTime? CurrentRotationEndDate { get; set; }

    #endregion

    #region Medical

    /// <summary>
    /// Blood type
    /// </summary>
    [MaxLength(10)]
    public string? BloodType { get; set; }

    /// <summary>
    /// Known allergies (JSON array)
    /// </summary>
    public string? AllergiesJson { get; set; }

    /// <summary>
    /// Medical conditions (JSON array)
    /// </summary>
    public string? MedicalConditionsJson { get; set; }

    /// <summary>
    /// Last medical examination date
    /// </summary>
    public DateTime? LastMedicalExamDate { get; set; }

    /// <summary>
    /// Medical fitness expiry date (ENG1 or equivalent)
    /// </summary>
    public DateTime? MedicalFitnessExpiryDate { get; set; }

    #endregion

    #region Photo

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    #endregion

    #region Notes

    public string? Notes { get; set; }

    public string? InternalNotes { get; set; }

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

    #region Navigation Properties

    [JsonIgnore]
    public virtual ICollection<VesselAssignment> VesselAssignments { get; set; } = new List<VesselAssignment>();

    [JsonIgnore]
    public virtual ICollection<PersonnelCertification> Certifications { get; set; } = new List<PersonnelCertification>();

    [JsonIgnore]
    public virtual ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

    [JsonIgnore]
    public virtual ICollection<Personnel> DirectReports { get; set; } = new List<Personnel>();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Years of service
    /// </summary>
    [NotMapped]
    public double YearsOfService => (DateTime.UtcNow - HireDate).TotalDays / 365.25;

    /// <summary>
    /// Whether passport is expiring soon (within 6 months)
    /// </summary>
    [NotMapped]
    public bool IsPassportExpiringSoon =>
        PassportExpiryDate.HasValue &&
        PassportExpiryDate.Value <= DateTime.Today.AddMonths(6);

    /// <summary>
    /// Whether passport is expired
    /// </summary>
    [NotMapped]
    public bool IsPassportExpired =>
        PassportExpiryDate.HasValue &&
        PassportExpiryDate.Value < DateTime.Today;

    /// <summary>
    /// Whether medical fitness is expiring soon (within 60 days)
    /// </summary>
    [NotMapped]
    public bool IsMedicalExpiringSoon =>
        MedicalFitnessExpiryDate.HasValue &&
        MedicalFitnessExpiryDate.Value <= DateTime.Today.AddDays(60);

    /// <summary>
    /// Whether medical fitness is expired
    /// </summary>
    [NotMapped]
    public bool IsMedicalExpired =>
        MedicalFitnessExpiryDate.HasValue &&
        MedicalFitnessExpiryDate.Value < DateTime.Today;

    /// <summary>
    /// Status display text
    /// </summary>
    [NotMapped]
    public string StatusDisplay => EmploymentStatus.ToString();

    #endregion
}
