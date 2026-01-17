using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents a contact person at a client company.
/// </summary>
public class ClientContact
{
    #region Identification

    [Key]
    public Guid ContactId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the client company
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Navigation property to the client
    /// </summary>
    [ForeignKey(nameof(ClientId))]
    [JsonIgnore]
    public virtual Client? Client { get; set; }

    #endregion

    #region Personal Information

    /// <summary>
    /// Contact's title (Mr., Ms., Dr., etc.)
    /// </summary>
    [MaxLength(20)]
    public string? Title { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Job title or position
    /// </summary>
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Department within the company
    /// </summary>
    [MaxLength(100)]
    public string? Department { get; set; }

    #endregion

    #region Contact Details

    /// <summary>
    /// Primary email address
    /// </summary>
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// Office phone number
    /// </summary>
    [MaxLength(30)]
    public string? Phone { get; set; }

    /// <summary>
    /// Mobile phone number
    /// </summary>
    [MaxLength(30)]
    public string? Mobile { get; set; }

    /// <summary>
    /// Fax number (still used in some regions)
    /// </summary>
    [MaxLength(30)]
    public string? Fax { get; set; }

    /// <summary>
    /// LinkedIn profile URL
    /// </summary>
    [MaxLength(500)]
    public string? LinkedInUrl { get; set; }

    #endregion

    #region Classification

    /// <summary>
    /// Type of contact (Primary, Technical, Commercial, etc.)
    /// </summary>
    public ContactType ContactType { get; set; } = ContactType.Primary;

    /// <summary>
    /// Is this the primary contact for the client
    /// </summary>
    public bool IsPrimaryContact { get; set; }

    /// <summary>
    /// Should this contact receive project communications
    /// </summary>
    public bool ReceiveProjectUpdates { get; set; } = true;

    /// <summary>
    /// Should this contact receive invoices
    /// </summary>
    public bool ReceiveInvoices { get; set; }

    /// <summary>
    /// Should this contact receive reports
    /// </summary>
    public bool ReceiveReports { get; set; }

    #endregion

    #region Location

    /// <summary>
    /// Office location if different from client HQ
    /// </summary>
    [MaxLength(200)]
    public string? OfficeLocation { get; set; }

    /// <summary>
    /// Timezone for scheduling
    /// </summary>
    [MaxLength(50)]
    public string? Timezone { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// Notes about this contact
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Preferred contact method
    /// </summary>
    [MaxLength(50)]
    public string? PreferredContactMethod { get; set; }

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
    public string DisplayName => !string.IsNullOrEmpty(JobTitle)
        ? $"{FullName} ({JobTitle})"
        : FullName;

    [NotMapped]
    public string FormalName => !string.IsNullOrEmpty(Title)
        ? $"{Title} {FullName}"
        : FullName;

    #endregion
}
