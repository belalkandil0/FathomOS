// FathomOS.Core/Models/Certificate.cs
// Domain model for processing certificates
// Maps to the Certificates table in SQLite (see Migration001_CreateCertificates.cs)

namespace FathomOS.Core.Models;

/// <summary>
/// Represents a processing certificate issued by a FathomOS module.
/// This is the complete domain model with all fields from the database schema.
/// For display/listing purposes, use CertificateSummary from ICertificationService.cs.
/// </summary>
public class Certificate
{
    #region Primary Identity

    /// <summary>
    /// Unique certificate identifier.
    /// Format: FOS-{LicenseeCode}-{YYMM}-{Sequence}-{Check}
    /// Example: FOS-OCS-2601-0001-X
    /// </summary>
    public required string CertificateId { get; set; }

    #endregion

    #region License Information

    /// <summary>
    /// The license ID this certificate was issued under
    /// </summary>
    public required string LicenseId { get; set; }

    /// <summary>
    /// Three-letter licensee code (e.g., "OCS", "ABC")
    /// Used for certificate ID generation and filtering
    /// </summary>
    public required string LicenseeCode { get; set; }

    #endregion

    #region Module Information

    /// <summary>
    /// Module identifier (e.g., "SurveyListing", "GNSS")
    /// </summary>
    public required string ModuleId { get; set; }

    /// <summary>
    /// Short code for the module used in certificate ID (e.g., "SLG", "GNS")
    /// </summary>
    public required string ModuleCertificateCode { get; set; }

    /// <summary>
    /// Version of the module that issued the certificate
    /// </summary>
    public required string ModuleVersion { get; set; }

    #endregion

    #region Certificate Metadata

    /// <summary>
    /// When the certificate was issued (UTC)
    /// </summary>
    public required DateTime IssuedAt { get; set; }

    /// <summary>
    /// Name of the project this certificate is for
    /// </summary>
    public required string ProjectName { get; set; }

    /// <summary>
    /// Location of the project (optional)
    /// </summary>
    public string? ProjectLocation { get; set; }

    /// <summary>
    /// Vessel name (optional)
    /// </summary>
    public string? Vessel { get; set; }

    /// <summary>
    /// Client name (optional)
    /// </summary>
    public string? Client { get; set; }

    #endregion

    #region Signatory Information

    /// <summary>
    /// Name of the person who signed/authorized the certificate
    /// </summary>
    public required string SignatoryName { get; set; }

    /// <summary>
    /// Title of the signatory (optional)
    /// </summary>
    public string? SignatoryTitle { get; set; }

    /// <summary>
    /// Company name of the signatory
    /// </summary>
    public required string CompanyName { get; set; }

    #endregion

    #region Processing Data

    /// <summary>
    /// Module-specific processing data as JSON string.
    /// Contains key-value pairs relevant to the module's processing.
    /// </summary>
    public string? ProcessingDataJson { get; set; }

    /// <summary>
    /// JSON array of input file names
    /// </summary>
    public string? InputFilesJson { get; set; }

    /// <summary>
    /// JSON array of output file names
    /// </summary>
    public string? OutputFilesJson { get; set; }

    #endregion

    #region Cryptographic Signature

    /// <summary>
    /// Cryptographic signature of the certificate data
    /// Used for verification and tamper detection
    /// </summary>
    public required string Signature { get; set; }

    /// <summary>
    /// Algorithm used for signing (e.g., "SHA256withECDSA")
    /// </summary>
    public required string SignatureAlgorithm { get; set; }

    /// <summary>
    /// SHA256 hash of the processed data for integrity verification
    /// </summary>
    public string? DataHash { get; set; }

    #endregion

    #region Sync Status

    /// <summary>
    /// Current sync status: "pending", "synced", or "failed"
    /// </summary>
    public required string SyncStatus { get; set; } = "pending";

    /// <summary>
    /// When the certificate was synced to the server (UTC)
    /// </summary>
    public DateTime? SyncedAt { get; set; }

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? SyncError { get; set; }

    /// <summary>
    /// Number of sync attempts made
    /// </summary>
    public int SyncAttempts { get; set; } = 0;

    #endregion

    #region Audit Fields

    /// <summary>
    /// When the certificate was created locally (UTC)
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the certificate was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    #endregion

    #region Verification & Branding

    /// <summary>
    /// URL for QR code verification
    /// Example: https://verify.fathom.io/c/FOS-OCS-2601-0001-X
    /// </summary>
    public string? QrCodeUrl { get; set; }

    /// <summary>
    /// Edition name from the license (e.g., "Professional", "Enterprise")
    /// </summary>
    public string? EditionName { get; set; }

    #endregion
}
