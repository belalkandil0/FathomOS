// FathomOS.Core/Interfaces/ICertificateRepository.cs
// Repository interface for certificate data access
// Extends ISyncableRepository with certificate-specific query methods

using FathomOS.Core.Data;
using FathomOS.Core.Models;

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Statistics for certificate sync operations
/// </summary>
public class CertificateSyncStatistics
{
    /// <summary>Total number of certificates in the local database</summary>
    public int TotalCount { get; init; }

    /// <summary>Number of certificates pending sync to server</summary>
    public int PendingCount { get; init; }

    /// <summary>Number of certificates successfully synced</summary>
    public int SyncedCount { get; init; }

    /// <summary>Number of certificates that failed to sync</summary>
    public int FailedCount { get; init; }

    /// <summary>Timestamp of the most recent sync (UTC)</summary>
    public DateTime? LastSyncAt { get; init; }
}

/// <summary>
/// Repository interface for certificate data access.
/// Extends ISyncableRepository with certificate-specific query methods.
/// </summary>
/// <remarks>
/// Implementation notes:
/// - All dates should be stored and returned as UTC
/// - CacheAsync is used for single-certificate caching during verification (not bulk sync)
/// - Certificates are immutable after creation - use sync status updates only
/// </remarks>
public interface ICertificateRepository : ISyncableRepository<Certificate>
{
    #region Certificate-Specific Queries

    /// <summary>
    /// Gets all certificates for a specific licensee code
    /// </summary>
    /// <param name="licenseeCode">The three-letter licensee code (e.g., "OCS")</param>
    /// <returns>Certificates matching the licensee code, ordered by IssuedAt descending</returns>
    Task<IEnumerable<Certificate>> GetByLicenseeCodeAsync(string licenseeCode);

    /// <summary>
    /// Gets all certificates issued by a specific module
    /// </summary>
    /// <param name="moduleId">The module identifier (e.g., "SurveyListing", "GNSS")</param>
    /// <returns>Certificates matching the module ID, ordered by IssuedAt descending</returns>
    Task<IEnumerable<Certificate>> GetByModuleIdAsync(string moduleId);

    /// <summary>
    /// Gets certificates issued within a date range
    /// </summary>
    /// <param name="start">Start of the date range (inclusive, UTC)</param>
    /// <param name="end">End of the date range (inclusive, UTC)</param>
    /// <returns>Certificates issued within the range, ordered by IssuedAt descending</returns>
    Task<IEnumerable<Certificate>> GetByDateRangeAsync(DateTime start, DateTime end);

    #endregion

    #region Verification Support

    /// <summary>
    /// Caches a single certificate locally for verification purposes.
    /// Used when verifying a certificate that exists on the server but not locally.
    /// This is NOT for bulk sync - only for single certificate verification caching.
    /// </summary>
    /// <param name="certificate">The certificate to cache</param>
    /// <remarks>
    /// Implementation should:
    /// - Store the certificate if it doesn't exist locally
    /// - Update the certificate if it already exists (server is source of truth for cached certs)
    /// - Mark the certificate as synced (since it came from the server)
    /// </remarks>
    Task CacheAsync(Certificate certificate);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets sync statistics for the certificate repository
    /// </summary>
    /// <returns>Statistics including counts of pending, synced, and failed certificates</returns>
    Task<CertificateSyncStatistics> GetSyncStatisticsAsync();

    #endregion

    #region Additional Queries

    /// <summary>
    /// Gets all certificates for a specific license ID
    /// </summary>
    /// <param name="licenseId">The license identifier</param>
    /// <returns>Certificates matching the license ID, ordered by IssuedAt descending</returns>
    Task<IEnumerable<Certificate>> GetByLicenseIdAsync(string licenseId);

    /// <summary>
    /// Gets all certificates for a specific project
    /// </summary>
    /// <param name="projectName">The project name</param>
    /// <returns>Certificates matching the project name, ordered by IssuedAt descending</returns>
    Task<IEnumerable<Certificate>> GetByProjectNameAsync(string projectName);

    /// <summary>
    /// Gets certificates that have failed to sync
    /// </summary>
    /// <returns>Certificates with failed sync status, ordered by SyncAttempts ascending</returns>
    Task<IEnumerable<Certificate>> GetFailedSyncAsync();

    /// <summary>
    /// Increments the sync attempt counter for a certificate
    /// </summary>
    /// <param name="certificateId">The certificate identifier</param>
    Task IncrementSyncAttemptsAsync(string certificateId);

    /// <summary>
    /// Resets sync status to pending for retry
    /// </summary>
    /// <param name="certificateId">The certificate identifier</param>
    Task ResetSyncStatusAsync(string certificateId);

    #endregion

    #region Sync Engine Support

    /// <summary>
    /// Gets certificates that have failed sync but are eligible for retry.
    /// </summary>
    /// <param name="maxAttempts">Maximum attempts threshold</param>
    /// <returns>Certificates with SyncAttempts less than maxAttempts</returns>
    Task<IEnumerable<Certificate>> GetRetryableCertificatesAsync(int maxAttempts);

    /// <summary>
    /// Updates the sync error message for a certificate.
    /// </summary>
    /// <param name="certificateId">The certificate ID</param>
    /// <param name="errorMessage">The error message</param>
    Task UpdateSyncErrorAsync(string certificateId, string errorMessage);

    /// <summary>
    /// Gets the current sync attempt count for a certificate.
    /// </summary>
    /// <param name="certificateId">The certificate ID</param>
    /// <returns>The current attempt count</returns>
    Task<int> GetSyncAttemptsAsync(string certificateId);

    #endregion
}
