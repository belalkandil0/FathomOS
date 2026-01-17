// FathomOS.Core/Data/CertificateSyncEngine.cs
// Certificate synchronization engine for offline-first pattern
// Syncs certificates UP to SQL Server when online (never pulls DOWN)

using System.Diagnostics;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Models;

namespace FathomOS.Core.Data;

#region Interfaces

/// <summary>
/// Interface for server communication during certificate sync.
/// Implementations handle the actual HTTP calls to the licensing server.
/// </summary>
public interface ISyncApiClient
{
    /// <summary>
    /// Pushes a single certificate to the server.
    /// </summary>
    /// <param name="certificate">The certificate to push</param>
    /// <returns>True if successfully synced, false otherwise</returns>
    Task<bool> PushCertificateAsync(Certificate certificate);

    /// <summary>
    /// Checks if the server is reachable.
    /// </summary>
    /// <returns>True if server is online and responding</returns>
    Task<bool> IsOnlineAsync();
}

/// <summary>
/// Interface for the certificate synchronization engine.
/// Handles syncing certificates from local SQLite to the remote SQL Server.
/// </summary>
public interface ICertificateSyncEngine
{
    /// <summary>
    /// Synchronizes all eligible certificates (pending and retryable).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing counts of succeeded, failed, and skipped certificates</returns>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes only pending certificates (not previously failed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing counts of succeeded, failed, and skipped certificates</returns>
    Task<SyncResult> SyncPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries synchronization for a specific certificate.
    /// </summary>
    /// <param name="certificateId">The certificate ID to retry</param>
    /// <returns>True if sync succeeded, false otherwise</returns>
    Task<bool> RetrySyncAsync(string certificateId);

    /// <summary>
    /// Checks if the server is reachable.
    /// </summary>
    /// <returns>True if server is online</returns>
    Task<bool> IsServerReachableAsync();

    /// <summary>
    /// Event fired when a certificate is successfully synced.
    /// </summary>
    event EventHandler<CertificateSyncedEventArgs>? CertificateSynced;

    /// <summary>
    /// Event fired when a certificate sync fails.
    /// </summary>
    event EventHandler<CertificateSyncFailedEventArgs>? CertificateSyncFailed;
}

#endregion

#region Models

/// <summary>
/// Result of a sync operation.
/// </summary>
/// <param name="Succeeded">Number of certificates successfully synced</param>
/// <param name="Failed">Number of certificates that failed to sync</param>
/// <param name="Skipped">Number of certificates skipped (e.g., exceeded max retries)</param>
public record SyncResult(int Succeeded, int Failed, int Skipped);

/// <summary>
/// Event arguments for successful certificate sync.
/// </summary>
public class CertificateSyncedEventArgs : EventArgs
{
    /// <summary>
    /// The synced certificate ID.
    /// </summary>
    public string CertificateId { get; }

    /// <summary>
    /// When the sync completed (UTC).
    /// </summary>
    public DateTime SyncedAt { get; }

    public CertificateSyncedEventArgs(string certificateId, DateTime syncedAt)
    {
        CertificateId = certificateId;
        SyncedAt = syncedAt;
    }
}

/// <summary>
/// Event arguments for failed certificate sync.
/// </summary>
public class CertificateSyncFailedEventArgs : EventArgs
{
    /// <summary>
    /// The certificate ID that failed to sync.
    /// </summary>
    public string CertificateId { get; }

    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Current sync attempt count.
    /// </summary>
    public int AttemptCount { get; }

    /// <summary>
    /// Whether max retries have been exceeded.
    /// </summary>
    public bool MaxRetriesExceeded { get; }

    public CertificateSyncFailedEventArgs(string certificateId, string errorMessage, int attemptCount, bool maxRetriesExceeded)
    {
        CertificateId = certificateId;
        ErrorMessage = errorMessage;
        AttemptCount = attemptCount;
        MaxRetriesExceeded = maxRetriesExceeded;
    }
}

#endregion

/// <summary>
/// Certificate synchronization engine implementing offline-first sync pattern.
/// Syncs certificates UP to SQL Server when online (never pulls DOWN).
/// Implements exponential backoff for failed syncs.
/// </summary>
public class CertificateSyncEngine : ICertificateSyncEngine
{
    #region Constants

    /// <summary>
    /// Maximum number of sync attempts before marking as "failed".
    /// </summary>
    public const int MaxSyncAttempts = 5;

    /// <summary>
    /// Base delay in seconds for exponential backoff (1s, 2s, 4s, 8s, max 60s).
    /// </summary>
    public const int BaseDelaySeconds = 1;

    /// <summary>
    /// Maximum delay in seconds for exponential backoff.
    /// </summary>
    public const int MaxDelaySeconds = 60;

    #endregion

    #region Fields

    private readonly ICertificateRepository _repository;
    private readonly ISyncApiClient _apiClient;

    #endregion

    #region Events

    /// <inheritdoc/>
    public event EventHandler<CertificateSyncedEventArgs>? CertificateSynced;

    /// <inheritdoc/>
    public event EventHandler<CertificateSyncFailedEventArgs>? CertificateSyncFailed;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new CertificateSyncEngine instance.
    /// </summary>
    /// <param name="repository">Certificate repository for database operations</param>
    /// <param name="apiClient">API client for server communication</param>
    /// <exception cref="ArgumentNullException">Thrown if repository or apiClient is null</exception>
    public CertificateSyncEngine(ICertificateRepository repository, ISyncApiClient apiClient)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        // Check if server is reachable first
        if (!await IsServerReachableAsync())
        {
            Debug.WriteLine("CertificateSyncEngine: Server not reachable, skipping sync.");
            return new SyncResult(0, 0, 0);
        }

        var succeeded = 0;
        var failed = 0;
        var skipped = 0;

        // Get all pending certificates
        var pendingCertificates = await _repository.GetPendingSyncAsync();
        foreach (var certificate in pendingCertificates)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("CertificateSyncEngine: Sync cancelled.");
                break;
            }

            var result = await SyncCertificateAsync(certificate);
            if (result == SyncOutcome.Succeeded)
                succeeded++;
            else if (result == SyncOutcome.Failed)
                failed++;
            else
                skipped++;
        }

        // Get retryable certificates (failed but under max attempts)
        var retryableCertificates = await _repository.GetRetryableCertificatesAsync(MaxSyncAttempts);
        foreach (var certificate in retryableCertificates)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("CertificateSyncEngine: Sync cancelled.");
                break;
            }

            // Apply exponential backoff delay
            var attempts = await _repository.GetSyncAttemptsAsync(certificate.CertificateId);
            var delaySeconds = CalculateBackoffDelay(attempts);

            Debug.WriteLine($"CertificateSyncEngine: Retrying {certificate.CertificateId} after {delaySeconds}s delay (attempt {attempts + 1}).");

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

            var result = await SyncCertificateAsync(certificate);
            if (result == SyncOutcome.Succeeded)
                succeeded++;
            else if (result == SyncOutcome.Failed)
                failed++;
            else
                skipped++;
        }

        Debug.WriteLine($"CertificateSyncEngine: Sync complete. Succeeded: {succeeded}, Failed: {failed}, Skipped: {skipped}.");
        return new SyncResult(succeeded, failed, skipped);
    }

    /// <inheritdoc/>
    public async Task<SyncResult> SyncPendingAsync(CancellationToken cancellationToken = default)
    {
        // Check if server is reachable first
        if (!await IsServerReachableAsync())
        {
            Debug.WriteLine("CertificateSyncEngine: Server not reachable, skipping sync.");
            return new SyncResult(0, 0, 0);
        }

        var succeeded = 0;
        var failed = 0;
        var skipped = 0;

        // Get only pending certificates (not retryable failed ones)
        var pendingCertificates = await _repository.GetPendingSyncAsync();
        foreach (var certificate in pendingCertificates)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("CertificateSyncEngine: Sync cancelled.");
                break;
            }

            var result = await SyncCertificateAsync(certificate);
            if (result == SyncOutcome.Succeeded)
                succeeded++;
            else if (result == SyncOutcome.Failed)
                failed++;
            else
                skipped++;
        }

        Debug.WriteLine($"CertificateSyncEngine: SyncPending complete. Succeeded: {succeeded}, Failed: {failed}, Skipped: {skipped}.");
        return new SyncResult(succeeded, failed, skipped);
    }

    /// <inheritdoc/>
    public async Task<bool> RetrySyncAsync(string certificateId)
    {
        if (string.IsNullOrEmpty(certificateId))
        {
            throw new ArgumentNullException(nameof(certificateId));
        }

        // Check if server is reachable
        if (!await IsServerReachableAsync())
        {
            Debug.WriteLine($"CertificateSyncEngine: Cannot retry {certificateId}, server not reachable.");
            return false;
        }

        // Get the certificate
        var certificate = await _repository.GetByIdAsync(certificateId);
        if (certificate == null)
        {
            Debug.WriteLine($"CertificateSyncEngine: Certificate {certificateId} not found.");
            return false;
        }

        // Check current attempt count
        var attempts = await _repository.GetSyncAttemptsAsync(certificateId);
        if (attempts >= MaxSyncAttempts)
        {
            Debug.WriteLine($"CertificateSyncEngine: Certificate {certificateId} has exceeded max retries ({attempts}/{MaxSyncAttempts}).");
            return false;
        }

        // Apply backoff delay
        var delaySeconds = CalculateBackoffDelay(attempts);
        Debug.WriteLine($"CertificateSyncEngine: Retrying {certificateId} after {delaySeconds}s delay (attempt {attempts + 1}).");
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        // Attempt sync
        var result = await SyncCertificateAsync(certificate);
        return result == SyncOutcome.Succeeded;
    }

    /// <inheritdoc/>
    public async Task<bool> IsServerReachableAsync()
    {
        try
        {
            return await _apiClient.IsOnlineAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CertificateSyncEngine: Error checking server connectivity: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Outcome of a single certificate sync attempt.
    /// </summary>
    private enum SyncOutcome
    {
        Succeeded,
        Failed,
        Skipped
    }

    /// <summary>
    /// Syncs a single certificate to the server.
    /// </summary>
    /// <param name="certificate">The certificate to sync</param>
    /// <returns>The outcome of the sync attempt</returns>
    private async Task<SyncOutcome> SyncCertificateAsync(Certificate certificate)
    {
        var certificateId = certificate.CertificateId;

        try
        {
            Debug.WriteLine($"CertificateSyncEngine: Syncing certificate {certificateId}...");

            // Push to server
            var success = await _apiClient.PushCertificateAsync(certificate);

            if (success)
            {
                // Mark as synced
                var syncedAt = DateTime.UtcNow;
                await _repository.MarkSyncedAsync(certificateId, syncedAt);

                Debug.WriteLine($"CertificateSyncEngine: Certificate {certificateId} synced successfully.");

                // Fire synced event
                OnCertificateSynced(new CertificateSyncedEventArgs(certificateId, syncedAt));

                return SyncOutcome.Succeeded;
            }
            else
            {
                // Server rejected the certificate
                return await HandleSyncFailureAsync(certificateId, "Server rejected the certificate");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CertificateSyncEngine: Error syncing certificate {certificateId}: {ex.Message}");
            return await HandleSyncFailureAsync(certificateId, ex.Message);
        }
    }

    /// <summary>
    /// Handles a sync failure by incrementing attempts and possibly marking as failed.
    /// </summary>
    /// <param name="certificateId">The certificate ID</param>
    /// <param name="errorMessage">The error message</param>
    /// <returns>The outcome (Failed or Skipped if max retries exceeded)</returns>
    private async Task<SyncOutcome> HandleSyncFailureAsync(string certificateId, string errorMessage)
    {
        // Increment attempt count
        await _repository.IncrementSyncAttemptsAsync(certificateId);
        var newAttemptCount = await _repository.GetSyncAttemptsAsync(certificateId);

        // Update error message
        await _repository.UpdateSyncErrorAsync(certificateId, errorMessage);

        var maxRetriesExceeded = newAttemptCount >= MaxSyncAttempts;

        if (maxRetriesExceeded)
        {
            // Mark as permanently failed
            await _repository.MarkSyncFailedAsync(certificateId, errorMessage);
            Debug.WriteLine($"CertificateSyncEngine: Certificate {certificateId} marked as failed after {newAttemptCount} attempts.");
        }

        // Fire failed event
        OnCertificateSyncFailed(new CertificateSyncFailedEventArgs(
            certificateId,
            errorMessage,
            newAttemptCount,
            maxRetriesExceeded));

        return maxRetriesExceeded ? SyncOutcome.Skipped : SyncOutcome.Failed;
    }

    /// <summary>
    /// Calculates the exponential backoff delay in seconds.
    /// Formula: min(BaseDelay * 2^attempts, MaxDelay)
    /// Results in: 1s, 2s, 4s, 8s, 16s, 32s, capped at 60s
    /// </summary>
    /// <param name="attemptCount">Number of previous attempts</param>
    /// <returns>Delay in seconds</returns>
    private static int CalculateBackoffDelay(int attemptCount)
    {
        // 2^attemptCount: 1, 2, 4, 8, 16, 32, 64...
        var delay = BaseDelaySeconds * (int)Math.Pow(2, attemptCount);
        return Math.Min(delay, MaxDelaySeconds);
    }

    /// <summary>
    /// Raises the CertificateSynced event.
    /// </summary>
    protected virtual void OnCertificateSynced(CertificateSyncedEventArgs e)
    {
        CertificateSynced?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the CertificateSyncFailed event.
    /// </summary>
    protected virtual void OnCertificateSyncFailed(CertificateSyncFailedEventArgs e)
    {
        CertificateSyncFailed?.Invoke(this, e);
    }

    #endregion
}
