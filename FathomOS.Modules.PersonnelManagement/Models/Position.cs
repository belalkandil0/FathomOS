using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a job position/role within the organization
/// </summary>
public class Position
{
    [Key]
    public Guid PositionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Position code (e.g., SRV-SR, ROV-SUP)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Position title (e.g., Senior Surveyor, ROV Supervisor)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Department this position belongs to
    /// </summary>
    public Department Department { get; set; } = Department.Operations;

    /// <summary>
    /// Detailed description of the position
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Position level/grade for hierarchy and pay scale
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Whether this is an offshore position
    /// </summary>
    public bool IsOffshore { get; set; } = true;

    /// <summary>
    /// Whether this position requires vessel assignment
    /// </summary>
    public bool RequiresVesselAssignment { get; set; } = true;

    /// <summary>
    /// Default rotation pattern for this position
    /// </summary>
    public Guid? DefaultRotationPatternId { get; set; }

    [ForeignKey(nameof(DefaultRotationPatternId))]
    [JsonIgnore]
    public virtual RotationPattern? DefaultRotationPattern { get; set; }

    /// <summary>
    /// Minimum years of experience required
    /// </summary>
    public int? MinYearsExperience { get; set; }

    /// <summary>
    /// Required certifications stored as JSON array of CertificationTypeIds
    /// </summary>
    public string? RequiredCertificationsJson { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether position is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    #region Audit

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long SyncVersion { get; set; }

    #endregion

    #region Navigation Properties

    [JsonIgnore]
    public virtual ICollection<Personnel> Personnel { get; set; } = new List<Personnel>();

    #endregion
}
