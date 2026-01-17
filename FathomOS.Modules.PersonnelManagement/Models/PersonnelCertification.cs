using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.PersonnelManagement.Models;

/// <summary>
/// Represents a certification held by a personnel member (STCW, safety, technical, etc.)
/// </summary>
public class PersonnelCertification
{
    [Key]
    public Guid PersonnelCertificationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Personnel who holds this certification
    /// </summary>
    public Guid PersonnelId { get; set; }

    [ForeignKey(nameof(PersonnelId))]
    [JsonIgnore]
    public virtual Personnel? Personnel { get; set; }

    /// <summary>
    /// Type of certification
    /// </summary>
    public Guid CertificationTypeId { get; set; }

    [ForeignKey(nameof(CertificationTypeId))]
    [JsonIgnore]
    public virtual CertificationType? CertificationType { get; set; }

    /// <summary>
    /// Certificate number as issued
    /// </summary>
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }

    /// <summary>
    /// Issuing authority/organization
    /// </summary>
    [MaxLength(200)]
    public string? IssuingAuthority { get; set; }

    /// <summary>
    /// Country where issued
    /// </summary>
    [MaxLength(100)]
    public string? IssuingCountry { get; set; }

    /// <summary>
    /// Date certificate was issued
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Date certificate expires (null = no expiry)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Current status of the certification
    /// </summary>
    public CertificationStatus Status { get; set; } = CertificationStatus.Valid;

    /// <summary>
    /// Date certification was verified
    /// </summary>
    public DateTime? VerifiedDate { get; set; }

    /// <summary>
    /// Who verified the certification
    /// </summary>
    public Guid? VerifiedBy { get; set; }

    /// <summary>
    /// Verification notes
    /// </summary>
    [MaxLength(500)]
    public string? VerificationNotes { get; set; }

    /// <summary>
    /// URL to scanned certificate document
    /// </summary>
    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// File name of the document
    /// </summary>
    [MaxLength(200)]
    public string? DocumentFileName { get; set; }

    /// <summary>
    /// Binary document data (for offline storage)
    /// </summary>
    public byte[]? DocumentData { get; set; }

    /// <summary>
    /// Date of last renewal
    /// </summary>
    public DateTime? LastRenewalDate { get; set; }

    /// <summary>
    /// Renewal reminder sent flag
    /// </summary>
    public bool RenewalReminderSent { get; set; } = false;

    /// <summary>
    /// Additional notes
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
    /// Days until expiry (null if no expiry date)
    /// </summary>
    [NotMapped]
    public int? DaysUntilExpiry => ExpiryDate.HasValue
        ? (int)(ExpiryDate.Value - DateTime.Today).TotalDays
        : null;

    /// <summary>
    /// Whether certification is expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;

    /// <summary>
    /// Whether certification is expiring soon (within 60 days)
    /// </summary>
    [NotMapped]
    public bool IsExpiringSoon => ExpiryDate.HasValue &&
        !IsExpired &&
        ExpiryDate.Value <= DateTime.Today.AddDays(60);

    /// <summary>
    /// Whether certification is valid (not expired and status is Valid)
    /// </summary>
    [NotMapped]
    public bool IsValid => Status == CertificationStatus.Valid && !IsExpired;

    /// <summary>
    /// Status display with expiry info
    /// </summary>
    [NotMapped]
    public string StatusDisplay
    {
        get
        {
            if (IsExpired) return "Expired";
            if (IsExpiringSoon) return $"Expiring in {DaysUntilExpiry} days";
            return Status.ToString();
        }
    }

    #endregion
}
