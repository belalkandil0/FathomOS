using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.ProjectManagement.Models;

/// <summary>
/// Represents a client company that contracts survey projects.
/// </summary>
public class Client
{
    #region Identification

    [Key]
    public Guid ClientId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique client code (e.g., CLI-0001)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ClientCode { get; set; } = string.Empty;

    /// <summary>
    /// SAP customer number for integration
    /// </summary>
    [MaxLength(50)]
    public string? SapCustomerNumber { get; set; }

    #endregion

    #region Company Information

    /// <summary>
    /// Official company name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Short name or trading name
    /// </summary>
    [MaxLength(100)]
    public string? ShortName { get; set; }

    /// <summary>
    /// Legal entity name if different from company name
    /// </summary>
    [MaxLength(200)]
    public string? LegalEntityName { get; set; }

    /// <summary>
    /// Type of client business
    /// </summary>
    public ClientType ClientType { get; set; } = ClientType.Other;

    /// <summary>
    /// Tax identification number
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Company registration number
    /// </summary>
    [MaxLength(50)]
    public string? RegistrationNumber { get; set; }

    #endregion

    #region Contact Information

    /// <summary>
    /// Primary phone number
    /// </summary>
    [MaxLength(30)]
    public string? Phone { get; set; }

    /// <summary>
    /// Primary email address
    /// </summary>
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// Company website URL
    /// </summary>
    [MaxLength(500)]
    public string? Website { get; set; }

    #endregion

    #region Address

    /// <summary>
    /// Street address line 1
    /// </summary>
    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Street address line 2
    /// </summary>
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// State or province
    /// </summary>
    [MaxLength(100)]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    #endregion

    #region Billing Information

    /// <summary>
    /// Billing address if different from main address
    /// </summary>
    [MaxLength(500)]
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Default currency for invoicing
    /// </summary>
    public CurrencyCode DefaultCurrency { get; set; } = CurrencyCode.USD;

    /// <summary>
    /// Payment terms (e.g., Net 30)
    /// </summary>
    [MaxLength(50)]
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Credit limit for this client
    /// </summary>
    public decimal? CreditLimit { get; set; }

    #endregion

    #region Relationship

    /// <summary>
    /// Date client relationship started
    /// </summary>
    public DateTime? RelationshipStartDate { get; set; }

    /// <summary>
    /// Internal account manager user ID
    /// </summary>
    public Guid? AccountManagerId { get; set; }

    /// <summary>
    /// Client rating (1-5 stars)
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Is this a preferred/key client
    /// </summary>
    public bool IsPreferred { get; set; }

    #endregion

    #region Notes

    /// <summary>
    /// General notes about the client
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Internal notes (not visible to client)
    /// </summary>
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

    /// <summary>
    /// Contacts associated with this client
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ClientContact> Contacts { get; set; } = new List<ClientContact>();

    /// <summary>
    /// Projects for this client
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<SurveyProject> Projects { get; set; } = new List<SurveyProject>();

    #endregion

    #region Computed Properties

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(ShortName) ? ShortName : CompanyName;

    [NotMapped]
    public string FullAddress => string.Join(", ",
        new[] { AddressLine1, AddressLine2, City, StateProvince, PostalCode, Country }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    #endregion
}
