// FathomOS.Core/Sync/SyncEngine.cs
// Generic synchronization engine for offline-first data sync
// Provides bidirectional sync with conflict resolution

using System.Diagnostics;
using FathomOS.Core.Data;
using FathomOS.Core.Data.Entities;
using FathomOS.Core.Logging;

namespace FathomOS.Core.Sync;

#region Enums and Event Args

/// <summary>
/// Status of the sync engine
/// </summary>
public enum SyncStatus
{
    /// <summary>Sync engine is idle</summary>
    Idle,
    /// <summary>Sync engine is currently syncing</summary>
    Syncing,
    /// <summary>Last sync completed successfully</summary>
    Completed,
    /// <summary>Last sync failed</summary>
    Failed,
    /// <summary>Sync was cancelled</summary>
    Cancelled,
    /// <summary>Sync is paused</summary>
    Paused,
    /// <summary>Server is offline</summary>
    Offline
}

/// <summary>
/// Direction of sync operation
/// </summary>
public enum SyncDirection
{
    /// <summary>Upload local changes to server</summary>
    Upload,
    /// <summary>Download server changes to local</summary>
    Download,
    /// <summary>Both upload and download</summary>
    Bidirectional
}

/// <summary>
/// Strategy for resolving conflicts
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Server changes win</summary>
    ServerWins,
    /// <summary>Local changes win</summary>
    LocalWins,
    /// <summary>Most recent change wins</summary>
    LastWriteWins,
    /// <summary>Manual resolution required</summary>
    Manual
}

/// <summary>
/// Event arguments for sync progress updates
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    /// <summary>Current sync status</summary>
    public SyncStatus Status { get; init; }

    /// <summary>Total items to sync</summary>
    public int TotalItems { get; init; }

    /// <summary>Items completed</summary>
    public int CompletedItems { get; init; }

    /// <summary>Current item being synced</summary>
    public string? CurrentItem { get; init; }

    /// <summary>Progress percentage (0-100)</summary>
    public int ProgressPercentage => TotalItems > 0
        ? (int)((CompletedItems / (double)TotalItems) * 100)
        : 0;

    /// <summary>Optional message</summary>
    public string? Message { get; init; }
}

/// <summary>
/// Event arguments for sync conflicts
/// </summary>
public class SyncConflictEventArgs : EventArgs
{
    /// <summary>Entity ID with conflict</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>Entity type name</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>Local version of the entity</summary>
    public object? LocalEntity { get; init; }

    /// <summary>Server version of the entity</summary>
    public object? ServerEntity { get; init; }

    /// <summary>Set to the resolved entity</summary>
    public object? ResolvedEntity { get; set; }

    /// <summary>Whether the conflict was resolved</summary>
    public bool IsResolved { get; set; }
}

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResult
{
    /// <summary>Whether the sync was successful</summary>
    public bool Success { get; init; }

    /// <summary>Number of items uploaded</summary>
    public int Uploaded { get; init; }

    /// <summary>Number of items downloaded</summary>
    public int Downloaded { get; init; }

    /// <summary>Number of conflicts</summary>
    public int Conflicts { get; init; }

    /// <summary>Number of conflicts resolved</summary>
    public int ConflictsResolved { get; init; }

    /// <summary>Number of errors</summary>
    public int Errors { get; init; }

    /// <summary>Error message if failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Duration of the sync operation</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>When the sync started</summary>
    public DateTime StartedAt { get; init; }

    /// <summary>When the sync completed</summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>Creates a successful result</summary>
    public static SyncResult Successful(int uploaded, int downloaded, TimeSpan duration) => new()
    {
        Success = true,
        Uploaded = uploaded,
        Downloaded = downloaded,
        Duration = duration,
        CompletedAt = DateTime.UtcNow
    };

    /// <summary>Creates a failed result</summary>
    public static SyncResult Failed(string error, TimeSpan duration) => new()
    {
        Success = false,
        ErrorMessage = error,
        Duration = duration,
        CompletedAt = DateTime.UtcNow
    };

    /// <summary>Creates a cancelled result</summary>
    public static SyncResult Cancelled(TimeSpan duration) => new()
    {
        Success = false,
        ErrorMessage = "Sync was cancelled",
        Duration = duration,
        CompletedAt = DateTime.UtcNow
    };
}

#endregion

#region Interfaces

/// <summary>
/// Interface for the synchronization engine
/// </summary>
public interface ISyncEngine
{
    /// <summary>Current sync status</summary>
    SyncStatus Status { get; }

    /// <summary>Time of last successful sync</summary>
    DateTime? LastSyncTime { get; }

    /// <summary>Performs a sync operation</summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Sync result</returns>
    Task<SyncResult> SyncAsync(CancellationToken ct = default);

    /// <summary>Event raised when sync progress changes</summary>
    event EventHandler<SyncProgressEventArgs>? SyncProgress;
}

/// <summary>
/// Interface for the sync API client
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISyncApiClient<T> where T : class
{
    /// <summary>Checks if the server is reachable</summary>
    Task<bool> IsOnlineAsync(CancellationToken ct = default);

    /// <summary>Pushes an entity to the server</summary>
    Task<bool> PushAsync(T entity, CancellationToken ct = default);

    /// <summary>Pushes multiple entities to the server</summary>
    Task<int> PushBatchAsync(IEnumerable<T> entities, CancellationToken ct = default);

    /// <summary>Pulls entities modified since the given version</summary>
    Task<IEnumerable<T>> PullAsync(long sinceVersion, CancellationToken ct = default);

    /// <summary>Gets the current server version</summary>
    Task<long> GetServerVersionAsync(CancellationToken ct = default);
}

#endregion

/// <summary>
/// Generic synchronization engine for offline-first data sync.
/// Supports bidirectional sync with conflict resolution and retry logic.
/// </summary>
/// <typeparam name="T">Entity type to sync (must implement ISyncableEntity)</typeparam>
public class SyncEngine<T> : ISyncEngine where T : class, ISyncableEntity, new()
{
    #region Constants

    /// <summary>Default batch size for sync operations</summary>
    public const int DefaultBatchSize = 50;

    /// <summary>Maximum sync attempts before failing</summary>
    public const int MaxSyncAttempts = 3;

    /// <summary>Base delay for exponential backoff (milliseconds)</summary>
    public const int BaseDelayMs = 500;

    #endregion

    #region Fields

    private readonly ISyncableRepository<T> _repository;
    private readonly ISyncApiClient<T> _apiClient;
    private readonly ILogger? _logger;
    private readonly SyncDirection _direction;
    private readonly ConflictResolutionStrategy _conflictStrategy;
    private readonly int _batchSize;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private SyncStatus _status = SyncStatus.Idle;
    private DateTime? _lastSyncTime;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public SyncStatus Status => _status;

    /// <inheritdoc/>
    public DateTime? LastSyncTime => _lastSyncTime;

    #endregion

    #region Events

    /// <inheritdoc/>
    public event EventHandler<SyncProgressEventArgs>? SyncProgress;

    /// <summary>Event raised when a conflict is detected</summary>
    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new sync engine
    /// </summary>
    /// <param name="repository">Repository for local storage</param>
    /// <param name="apiClient">API client for server communication</param>
    /// <param name="logger">Optional logger</param>
    /// <param name="direction">Sync direction (default: Bidirectional)</param>
    /// <param name="conflictStrategy">Conflict resolution strategy (default: ServerWins)</param>
    /// <param name="batchSize">Batch size for sync operations (default: 50)</param>
    public SyncEngine(
        ISyncableRepository<T> repository,
        ISyncApiClient<T> apiClient,
        ILogger? logger = null,
        SyncDirection direction = SyncDirection.Bidirectional,
        ConflictResolutionStrategy conflictStrategy = ConflictResolutionStrategy.ServerWins,
        int batchSize = DefaultBatchSize)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
        _direction = direction;
        _conflictStrategy = conflictStrategy;
        _batchSize = batchSize > 0 ? batchSize : DefaultBatchSize;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
    {
        // Ensure only one sync runs at a time
        if (!await _syncLock.WaitAsync(0, ct))
        {
            _logger?.Warning("Sync already in progress, skipping.", null, "SyncEngine");
            return SyncResult.Failed("Sync already in progress", TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();
        var startedAt = DateTime.UtcNow;

        try
        {
            _status = SyncStatus.Syncing;
            OnSyncProgress(new SyncProgressEventArgs
            {
                Status = SyncStatus.Syncing,
                Message = "Starting sync..."
            });

            // Check server connectivity
            if (!await CheckConnectivityAsync(ct))
            {
                _status = SyncStatus.Offline;
                return SyncResult.Failed("Server is offline", stopwatch.Elapsed);
            }

            int uploaded = 0, downloaded = 0, conflicts = 0, conflictsResolved = 0;

            // Upload local changes
            if (_direction is SyncDirection.Upload or SyncDirection.Bidirectional)
            {
                var uploadResult = await UploadChangesAsync(ct);
                uploaded = uploadResult.Succeeded;
            }

            // Download server changes
            if (_direction is SyncDirection.Download or SyncDirection.Bidirectional)
            {
                var downloadResult = await DownloadChangesAsync(ct);
                downloaded = downloadResult.Downloaded;
                conflicts = downloadResult.Conflicts;
                conflictsResolved = downloadResult.ConflictsResolved;
            }

            stopwatch.Stop();
            _status = SyncStatus.Completed;
            _lastSyncTime = DateTime.UtcNow;

            OnSyncProgress(new SyncProgressEventArgs
            {
                Status = SyncStatus.Completed,
                Message = $"Sync completed. Uploaded: {uploaded}, Downloaded: {downloaded}"
            });

            _logger?.Info($"Sync completed. Uploaded: {uploaded}, Downloaded: {downloaded}, Conflicts: {conflicts}", "SyncEngine");

            return new SyncResult
            {
                Success = true,
                Uploaded = uploaded,
                Downloaded = downloaded,
                Conflicts = conflicts,
                ConflictsResolved = conflictsResolved,
                Duration = stopwatch.Elapsed,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _status = SyncStatus.Cancelled;
            _logger?.Warning("Sync was cancelled.", null, "SyncEngine");
            return SyncResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _status = SyncStatus.Failed;
            _logger?.Error($"Sync failed: {ex.Message}", ex, "SyncEngine");
            return SyncResult.Failed(ex.Message, stopwatch.Elapsed);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Forces a full sync, ignoring cached sync times
    /// </summary>
    public async Task<SyncResult> ForceSyncAsync(CancellationToken ct = default)
    {
        _lastSyncTime = null;
        return await SyncAsync(ct);
    }

    /// <summary>
    /// Pauses sync operations
    /// </summary>
    public void Pause()
    {
        if (_status == SyncStatus.Syncing)
        {
            _status = SyncStatus.Paused;
        }
    }

    /// <summary>
    /// Resumes sync operations
    /// </summary>
    public void Resume()
    {
        if (_status == SyncStatus.Paused)
        {
            _status = SyncStatus.Idle;
        }
    }

    #endregion

    #region Private Methods

    private async Task<bool> CheckConnectivityAsync(CancellationToken ct)
    {
        try
        {
            return await _apiClient.IsOnlineAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Connectivity check failed: {ex.Message}", ex, "SyncEngine");
            return false;
        }
    }

    private async Task<(int Succeeded, int Failed)> UploadChangesAsync(CancellationToken ct)
    {
        var pending = (await _repository.GetPendingSyncAsync()).ToList();
        if (!pending.Any())
        {
            return (0, 0);
        }

        _logger?.Info($"Uploading {pending.Count} pending items...", "SyncEngine");

        int succeeded = 0, failed = 0;
        var batches = pending.Chunk(_batchSize);
        var totalItems = pending.Count;
        var completedItems = 0;

        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var entity in batch)
            {
                var success = await UploadWithRetryAsync(entity, ct);
                if (success)
                {
                    await _repository.MarkSyncedAsync(GetEntityId(entity), DateTime.UtcNow);
                    succeeded++;
                }
                else
                {
                    await _repository.MarkSyncFailedAsync(GetEntityId(entity), "Upload failed after retries");
                    failed++;
                }

                completedItems++;
                OnSyncProgress(new SyncProgressEventArgs
                {
                    Status = SyncStatus.Syncing,
                    TotalItems = totalItems,
                    CompletedItems = completedItems,
                    CurrentItem = GetEntityId(entity),
                    Message = $"Uploading {completedItems}/{totalItems}..."
                });
            }
        }

        return (succeeded, failed);
    }

    private async Task<bool> UploadWithRetryAsync(T entity, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= MaxSyncAttempts; attempt++)
        {
            try
            {
                if (await _apiClient.PushAsync(entity, ct))
                {
                    return true;
                }
            }
            catch (Exception ex) when (attempt < MaxSyncAttempts)
            {
                _logger?.Warning(
                    $"Upload attempt {attempt}/{MaxSyncAttempts} failed for entity {GetEntityId(entity)}: {ex.Message}",
                    ex, "SyncEngine");

                var delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay, ct);
            }
        }

        return false;
    }

    private async Task<(int Downloaded, int Conflicts, int ConflictsResolved)> DownloadChangesAsync(CancellationToken ct)
    {
        // Get server changes since last sync
        long sinceVersion = 0;
        if (_lastSyncTime.HasValue)
        {
            // Convert last sync time to a version number (using ticks as version)
            sinceVersion = _lastSyncTime.Value.Ticks;
        }

        IEnumerable<T> serverChanges;
        try
        {
            serverChanges = await _apiClient.PullAsync(sinceVersion, ct);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to pull changes from server: {ex.Message}", ex, "SyncEngine");
            return (0, 0, 0);
        }

        var changesList = serverChanges.ToList();
        if (!changesList.Any())
        {
            return (0, 0, 0);
        }

        _logger?.Info($"Downloaded {changesList.Count} items from server...", "SyncEngine");

        int downloaded = 0, conflicts = 0, conflictsResolved = 0;

        foreach (var serverEntity in changesList)
        {
            ct.ThrowIfCancellationRequested();

            var entityId = GetEntityId(serverEntity);
            var localEntity = await GetLocalEntityByIdAsync(entityId);

            if (localEntity == null)
            {
                // New entity from server - just add it
                await _repository.AddAsync(serverEntity);
                await _repository.MarkSyncedAsync(entityId, DateTime.UtcNow);
                downloaded++;
            }
            else if (localEntity.HasPendingChanges)
            {
                // Conflict - local has unsync'd changes
                conflicts++;
                var resolved = await ResolveConflictAsync(localEntity, serverEntity);
                if (resolved != null)
                {
                    await _repository.UpdateAsync(resolved);
                    await _repository.MarkSyncedAsync(entityId, DateTime.UtcNow);
                    conflictsResolved++;
                    downloaded++;
                }
            }
            else
            {
                // No conflict - update local with server version
                await _repository.UpdateAsync(serverEntity);
                await _repository.MarkSyncedAsync(entityId, DateTime.UtcNow);
                downloaded++;
            }
        }

        return (downloaded, conflicts, conflictsResolved);
    }

    private async Task<T?> ResolveConflictAsync(T localEntity, T serverEntity)
    {
        switch (_conflictStrategy)
        {
            case ConflictResolutionStrategy.ServerWins:
                return serverEntity;

            case ConflictResolutionStrategy.LocalWins:
                return localEntity;

            case ConflictResolutionStrategy.LastWriteWins:
                // Compare modification timestamps
                if (localEntity is IAuditableEntity localAuditable && serverEntity is IAuditableEntity serverAuditable)
                {
                    var localTime = localAuditable.ModifiedAt ?? localAuditable.CreatedAt;
                    var serverTime = serverAuditable.ModifiedAt ?? serverAuditable.CreatedAt;
                    return localTime > serverTime ? localEntity : serverEntity;
                }
                // Fall back to server wins if timestamps not available
                return serverEntity;

            case ConflictResolutionStrategy.Manual:
                // Raise event for manual resolution
                var args = new SyncConflictEventArgs
                {
                    EntityId = GetEntityId(localEntity),
                    EntityType = typeof(T).Name,
                    LocalEntity = localEntity,
                    ServerEntity = serverEntity
                };

                OnConflictDetected(args);

                if (args.IsResolved && args.ResolvedEntity is T resolved)
                {
                    return resolved;
                }

                // If not resolved, skip this entity
                _logger?.Warning($"Conflict for entity {args.EntityId} was not resolved.", null, "SyncEngine");
                return null;

            default:
                return serverEntity;
        }
    }

    private async Task<T?> GetLocalEntityByIdAsync(string id)
    {
        // Try to get by string ID first, fall back to Guid
        if (typeof(IEntity).IsAssignableFrom(typeof(T)))
        {
            if (Guid.TryParse(id, out var guidId))
            {
                var all = await _repository.GetAllAsync();
                return all.FirstOrDefault(e => ((IEntity)e).Id == guidId);
            }
        }

        // For entities with string IDs, search through all
        var entities = await _repository.GetAllAsync();
        return entities.FirstOrDefault(e => GetEntityId(e) == id);
    }

    private static string GetEntityId(T entity)
    {
        if (entity is IEntity entityWithId)
        {
            return entityWithId.Id.ToString();
        }

        // Try to find an Id property via reflection
        var idProp = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty($"{typeof(T).Name}Id");
        return idProp?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }

    protected virtual void OnSyncProgress(SyncProgressEventArgs e)
    {
        SyncProgress?.Invoke(this, e);
    }

    protected virtual void OnConflictDetected(SyncConflictEventArgs e)
    {
        ConflictDetected?.Invoke(this, e);
    }

    #endregion
}

/// <summary>
/// Extension methods for configuring sync behavior
/// </summary>
public static class SyncEngineExtensions
{
    /// <summary>
    /// Creates a sync engine with upload-only direction
    /// </summary>
    public static SyncEngine<T> CreateUploadOnlyEngine<T>(
        this ISyncableRepository<T> repository,
        ISyncApiClient<T> apiClient,
        ILogger? logger = null) where T : class, ISyncableEntity, new()
    {
        return new SyncEngine<T>(
            repository,
            apiClient,
            logger,
            SyncDirection.Upload);
    }

    /// <summary>
    /// Creates a sync engine with download-only direction
    /// </summary>
    public static SyncEngine<T> CreateDownloadOnlyEngine<T>(
        this ISyncableRepository<T> repository,
        ISyncApiClient<T> apiClient,
        ILogger? logger = null) where T : class, ISyncableEntity, new()
    {
        return new SyncEngine<T>(
            repository,
            apiClient,
            logger,
            SyncDirection.Download);
    }
}
