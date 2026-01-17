using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a type of certification (e.g., STCW Basic Safety, BOSIET)
/// </summary>
public class CertificationType
{
    [Key]
    public Guid CertificationTypeId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Certification code (e.g., STCW-BST, BOSIET, HUET)
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Certification name (e.g., "Basic Safety Training", "BOSIET")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short name for display
    /// </summary>
    [MaxLength(50)]
    public string? ShortName { get; set; }

    /// <summary>
    /// Detailed description of the certification
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category of certification
    /// </summary>
    public CertificationCategory Category { get; set; } = CertificationCategory.STCW;

    /// <summary>
    /// Issuing authority or organization
    /// </summary>
    [MaxLength(200)]
    public string? IssuingAuthority { get; set; }

    /// <summary>
    /// Default validity period in months (null = no expiry)
    /// </summary>
    public int? ValidityMonths { get; set; }

    /// <summary>
    /// Number of days before expiry to start warning
    /// </summary>
    public int WarningDaysBeforeExpiry { get; set; } = 60;

    /// <summary>
    /// Whether this certification is mandatory for offshore work
    /// </summary>
    public bool IsMandatory { get; set; } = false;

    /// <summary>
    /// Whether this certification requires renewal training
    /// </summary>
    public bool RequiresRenewalTraining { get; set; } = true;

    /// <summary>
    /// Prerequisite certifications (JSON array of CertificationTypeIds)
    /// </summary>
    public string? PrerequisitesJson { get; set; }

    /// <summary>
    /// Countries/flag states where this certification is recognized (JSON array)
    /// </summary>
    public string? RecognizedInJson { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether certification type is active
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
    public virtual ICollection<PersonnelCertification> PersonnelCertifications { get; set; } = new List<PersonnelCertification>();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Display name with code
    /// </summary>
    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(ShortName) ? ShortName : Name;

    /// <summary>
    /// Full display with code
    /// </summary>
    [NotMapped]
    public string FullDisplayName => $"{Code} - {Name}";

    #endregion
}
