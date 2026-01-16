using System.Windows;

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Request model for creating a new certificate
/// </summary>
public class CertificationRequest
{
    /// <summary>Module identifier (e.g., "SurveyListing")</summary>
    public required string ModuleId { get; init; }

    /// <summary>Short code for certificate ID (e.g., "SL")</summary>
    public required string ModuleCertificateCode { get; init; }

    /// <summary>Module version string</summary>
    public required string ModuleVersion { get; init; }

    /// <summary>Project name</summary>
    public required string ProjectName { get; init; }

    /// <summary>SHA256 hash of processed data for verification</summary>
    public string? DataHash { get; init; }

    /// <summary>Module-specific processing data</summary>
    public Dictionary<string, string>? ProcessingData { get; init; }

    /// <summary>List of input file names</summary>
    public List<string>? InputFiles { get; init; }

    /// <summary>List of output file names</summary>
    public List<string>? OutputFiles { get; init; }

    /// <summary>Project location</summary>
    public string? ProjectLocation { get; init; }

    /// <summary>Vessel name</summary>
    public string? VesselName { get; init; }

    /// <summary>Client name</summary>
    public string? ClientName { get; init; }
}

/// <summary>
/// Signatory information for certificate
/// </summary>
public class CertificateSignatory
{
    public required string Name { get; init; }
    public string? Title { get; init; }
    public required string CompanyName { get; init; }
}

/// <summary>
/// Result of certificate verification
/// </summary>
public class CertificateVerificationResult
{
    public bool IsValid { get; init; }
    public bool WasFound { get; init; }
    public string? CertificateId { get; init; }
    public string? ModuleId { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? VerificationMessage { get; init; }
}

/// <summary>
/// Sync status for certificates
/// </summary>
public enum CertificateSyncStatus
{
    Pending,
    Synced,
    Failed
}

/// <summary>
/// Certificate summary for listing
/// </summary>
public class CertificateSummary
{
    public required string CertificateId { get; init; }
    public required string ModuleId { get; init; }
    public required string ProjectName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public CertificateSyncStatus SyncStatus { get; init; }
    public string? ClientCode { get; init; }
}

/// <summary>
/// Contract for certificate generation, storage, and verification.
/// Provides DI-friendly access to the certification system.
/// </summary>
public interface ICertificationService
{
    /// <summary>
    /// Create a certificate with UI dialog for signatory information
    /// </summary>
    /// <param name="request">Certificate request details</param>
    /// <param name="owner">Parent window for dialog</param>
    /// <returns>Certificate ID if created, null if cancelled</returns>
    Task<string?> CreateWithDialogAsync(CertificationRequest request, Window? owner = null);

    /// <summary>
    /// Create a certificate without UI (for automated workflows)
    /// </summary>
    /// <param name="request">Certificate request details</param>
    /// <param name="signatory">Signatory information</param>
    /// <returns>Certificate ID</returns>
    Task<string> CreateSilentAsync(CertificationRequest request, CertificateSignatory signatory);

    /// <summary>
    /// Verify a certificate by its ID
    /// </summary>
    /// <param name="certificateId">The certificate ID to verify</param>
    /// <returns>Verification result</returns>
    Task<CertificateVerificationResult> VerifyAsync(string certificateId);

    /// <summary>
    /// Get all certificates pending sync
    /// </summary>
    Task<IEnumerable<CertificateSummary>> GetPendingSyncAsync();

    /// <summary>
    /// Get all local certificates
    /// </summary>
    Task<IEnumerable<CertificateSummary>> GetAllCertificatesAsync();

    /// <summary>
    /// Sync pending certificates to server
    /// </summary>
    /// <returns>Number of certificates synced</returns>
    Task<int> SyncAsync();

    /// <summary>
    /// Open the certificate manager window
    /// </summary>
    /// <param name="owner">Parent window</param>
    void OpenCertificateManager(Window? owner = null);

    /// <summary>
    /// View a specific certificate
    /// </summary>
    /// <param name="certificateId">The certificate to view</param>
    /// <param name="owner">Parent window</param>
    void ViewCertificate(string certificateId, Window? owner = null);

    /// <summary>
    /// Generate QR code URL for a certificate
    /// </summary>
    /// <param name="certificateId">The certificate ID</param>
    /// <returns>URL for QR code verification</returns>
    string GetVerificationUrl(string certificateId);

    /// <summary>
    /// Get the client code from the current license
    /// </summary>
    string? GetClientCode();

    /// <summary>
    /// Event fired when a certificate is created
    /// </summary>
    event EventHandler<string>? CertificateCreated;

    /// <summary>
    /// Event fired when certificates are synced
    /// </summary>
    event EventHandler<int>? CertificatesSynced;
}
