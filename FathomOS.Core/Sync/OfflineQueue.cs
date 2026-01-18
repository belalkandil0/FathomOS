// FathomOS.Core/Sync/OfflineQueue.cs
// Offline operation queue for storing operations when offline
// Persists operations to SQLite for replay when connectivity is restored

using Microsoft.Data.Sqlite;
using System.Text.Json;
using FathomOS.Core.Data;
using FathomOS.Core.Logging;

namespace FathomOS.Core.Sync;

#region Models

/// <summary>
/// Type of offline operation
/// </summary>
public enum OfflineOperationType
{
    /// <summary>Create a new entity</summary>
    Create,
    /// <summary>Update an existing entity</summary>
    Update,
    /// <summary>Delete an entity</summary>
    Delete,
    /// <summary>Custom operation</summary>
    Custom
}

/// <summary>
/// Status of an offline operation
/// </summary>
public enum OfflineOperationStatus
{
    /// <summary>Operation is pending execution</summary>
    Pending,
    /// <summary>Operation is currently being processed</summary>
    Processing,
    /// <summary>Operation completed successfully</summary>
    Completed,
    /// <summary>Operation failed</summary>
    Failed,
    /// <summary>Operation was cancelled</summary>
    Cancelled
}

/// <summary>
/// Represents an operation queued for offline execution
/// </summary>
public class OfflineOperation
{
    /// <summary>Unique identifier for this operation</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Type of operation</summary>
    public OfflineOperationType OperationType { get; set; }

    /// <summary>Entity type name (e.g., "Certificate")</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Entity ID being operated on</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON serialized payload data</summary>
    public string? PayloadJson { get; set; }

    /// <summary>Current status of the operation</summary>
    public OfflineOperationStatus Status { get; set; } = OfflineOperationStatus.Pending;

    /// <summary>Number of execution attempts</summary>
    public int Attempts { get; set; }

    /// <summary>Maximum allowed attempts</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>When the operation was created (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the operation was last attempted (UTC)</summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>When the operation was completed (UTC)</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Error message if failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Priority (lower = higher priority)</summary>
    public int Priority { get; set; } = 100;

    /// <summary>Optional correlation ID for grouping related operations</summary>
    public string? CorrelationId { get; set; }

    /// <summary>User ID who created this operation</summary>
    public Guid? UserId { get; set; }

    /// <summary>Deserializes the payload to the specified type</summary>
    public T? GetPayload<T>() where T : class
    {
        if (string.IsNullOrEmpty(PayloadJson))
            return null;

        return JsonSerializer.Deserialize<T>(PayloadJson);
    }

    /// <summary>Sets the payload from an object</summary>
    public void SetPayload<T>(T payload) where T : class
    {
        PayloadJson = JsonSerializer.Serialize(payload);
    }
}

/// <summary>
/// Statistics for the offline queue
/// </summary>
public class OfflineQueueStatistics
{
    /// <summary>Total operations in queue</summary>
    public int TotalCount { get; init; }

    /// <summary>Operations pending execution</summary>
    public int PendingCount { get; init; }

    /// <summary>Operations that failed</summary>
    public int FailedCount { get; init; }

    /// <summary>Operations completed</summary>
    public int CompletedCount { get; init; }

    /// <summary>Oldest pending operation timestamp</summary>
    public DateTime? OldestPendingAt { get; init; }
}

#endregion

#region Interface

/// <summary>
/// Interface for the offline operation queue
/// </summary>
public interface IOfflineQueue
{
    /// <summary>Adds an operation to the queue</summary>
    /// <param name="operation">Operation to queue</param>
    Task EnqueueAsync(OfflineOperation operation);

    /// <summary>Gets all pending operations ordered by priority and creation time</summary>
    Task<IEnumerable<OfflineOperation>> GetPendingAsync();

    /// <summary>Marks an operation as processed (completed or failed)</summary>
    /// <param name="operationId">Operation ID</param>
    Task MarkProcessedAsync(Guid operationId);

    /// <summary>Gets an operation by its ID</summary>
    Task<OfflineOperation?> GetByIdAsync(Guid operationId);

    /// <summary>Gets the count of pending operations</summary>
    Task<int> GetPendingCountAsync();

    /// <summary>Clears all completed operations</summary>
    Task ClearCompletedAsync();
}

#endregion

/// <summary>
/// SQLite implementation of the offline operation queue.
/// Stores operations in a persistent queue for replay when connectivity is restored.
/// </summary>
public class OfflineQueue : IOfflineQueue
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger? _logger;
    private readonly RetryPolicy _retryPolicy;
    private const string TableName = "OfflineQueue";

    /// <summary>
    /// Creates a new offline queue
    /// </summary>
    /// <param name="connectionFactory">SQLite connection factory</param>
    /// <param name="logger">Optional logger</param>
    /// <param name="retryPolicy">Optional retry policy</param>
    public OfflineQueue(
        SqliteConnectionFactory connectionFactory,
        ILogger? logger = null,
        RetryPolicy? retryPolicy = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.Default;
    }

    /// <summary>
    /// Ensures the offline queue table exists
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {TableName} (
                Id TEXT PRIMARY KEY NOT NULL,
                OperationType TEXT NOT NULL,
                EntityType TEXT NOT NULL,
                EntityId TEXT NOT NULL,
                PayloadJson TEXT,
                Status TEXT NOT NULL DEFAULT 'Pending',
                Attempts INTEGER NOT NULL DEFAULT 0,
                MaxAttempts INTEGER NOT NULL DEFAULT 5,
                CreatedAt TEXT NOT NULL,
                LastAttemptAt TEXT,
                CompletedAt TEXT,
                ErrorMessage TEXT,
                Priority INTEGER NOT NULL DEFAULT 100,
                CorrelationId TEXT,
                UserId TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_offlinequeue_status ON {TableName}(Status);
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_priority ON {TableName}(Priority, CreatedAt);
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_entitytype ON {TableName}(EntityType, EntityId);
            CREATE INDEX IF NOT EXISTS idx_offlinequeue_correlation ON {TableName}(CorrelationId);
        ";

        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task EnqueueAsync(OfflineOperation operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = $@"
                        INSERT INTO {TableName} (
                            Id, OperationType, EntityType, EntityId, PayloadJson,
                            Status, Attempts, MaxAttempts, CreatedAt, LastAttemptAt,
                            CompletedAt, ErrorMessage, Priority, CorrelationId, UserId
                        ) VALUES (
                            @id, @operationType, @entityType, @entityId, @payloadJson,
                            @status, @attempts, @maxAttempts, @createdAt, @lastAttemptAt,
                            @completedAt, @errorMessage, @priority, @correlationId, @userId
                        );";

                    AddOperationParameters(command, operation);
                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    _logger?.Info($"Enqueued operation {operation.Id} ({operation.OperationType} {operation.EntityType})", "OfflineQueue");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            $"EnqueueAsync {operation.Id}");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OfflineOperation>> GetPendingAsync()
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                var operations = new List<OfflineOperation>();
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $@"
                    SELECT * FROM {TableName}
                    WHERE Status = 'Pending' OR Status = 'Failed'
                    ORDER BY Priority ASC, CreatedAt ASC;";

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var operation = MapFromReader(reader);
                    if (operation.Status == OfflineOperationStatus.Failed && operation.Attempts >= operation.MaxAttempts)
                    {
                        // Skip failed operations that have exceeded max attempts
                        continue;
                    }
                    operations.Add(operation);
                }

                return operations;
            },
            _retryPolicy,
            _logger,
            "GetPendingAsync");
    }

    /// <inheritdoc/>
    public async Task MarkProcessedAsync(Guid operationId)
    {
        await UpdateStatusAsync(operationId, OfflineOperationStatus.Completed);
    }

    /// <summary>
    /// Marks an operation as failed with an error message
    /// </summary>
    public async Task MarkFailedAsync(Guid operationId, string? errorMessage = null)
    {
        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = $@"
                        UPDATE {TableName}
                        SET Status = 'Failed',
                            Attempts = Attempts + 1,
                            LastAttemptAt = @lastAttemptAt,
                            ErrorMessage = @errorMessage
                        WHERE Id = @id;";

                    command.Parameters.AddWithValue("@id", operationId.ToString());
                    command.Parameters.AddWithValue("@lastAttemptAt", DateTime.UtcNow.ToString("O"));
                    command.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    _logger?.Warning($"Operation {operationId} marked as failed: {errorMessage}", null, "OfflineQueue");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            $"MarkFailedAsync {operationId}");
    }

    /// <summary>
    /// Marks an operation as being processed
    /// </summary>
    public async Task MarkProcessingAsync(Guid operationId)
    {
        await UpdateStatusAsync(operationId, OfflineOperationStatus.Processing);
    }

    /// <inheritdoc/>
    public async Task<OfflineOperation?> GetByIdAsync(Guid operationId)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT * FROM {TableName} WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", operationId.ToString());

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapFromReader(reader);
                }

                return null;
            },
            _retryPolicy,
            _logger,
            $"GetByIdAsync {operationId}");
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingCountAsync()
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $@"
                    SELECT COUNT(*) FROM {TableName}
                    WHERE Status = 'Pending' OR (Status = 'Failed' AND Attempts < MaxAttempts);";

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            },
            _retryPolicy,
            _logger,
            "GetPendingCountAsync");
    }

    /// <inheritdoc/>
    public async Task ClearCompletedAsync()
    {
        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = $"DELETE FROM {TableName} WHERE Status = 'Completed';";
                    var deleted = await command.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    _logger?.Info($"Cleared {deleted} completed operations from offline queue.", "OfflineQueue");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            "ClearCompletedAsync");
    }

    /// <summary>
    /// Gets queue statistics
    /// </summary>
    public async Task<OfflineQueueStatistics> GetStatisticsAsync()
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();

                int total = 0, pending = 0, failed = 0, completed = 0;
                DateTime? oldestPending = null;

                // Get counts
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                        SELECT
                            COUNT(*) as TotalCount,
                            SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as PendingCount,
                            SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
                            SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) as CompletedCount
                        FROM {TableName};";

                    await using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        total = reader.GetInt32(0);
                        pending = reader.GetInt32(1);
                        failed = reader.GetInt32(2);
                        completed = reader.GetInt32(3);
                    }
                }

                // Get oldest pending timestamp
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                        SELECT MIN(CreatedAt) FROM {TableName}
                        WHERE Status = 'Pending';";

                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        oldestPending = DateTime.Parse((string)result, null,
                            System.Globalization.DateTimeStyles.RoundtripKind);
                    }
                }

                return new OfflineQueueStatistics
                {
                    TotalCount = total,
                    PendingCount = pending,
                    FailedCount = failed,
                    CompletedCount = completed,
                    OldestPendingAt = oldestPending
                };
            },
            _retryPolicy,
            _logger,
            "GetStatisticsAsync");
    }

    /// <summary>
    /// Gets all operations for a specific entity
    /// </summary>
    public async Task<IEnumerable<OfflineOperation>> GetByEntityAsync(string entityType, string entityId)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                var operations = new List<OfflineOperation>();
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $@"
                    SELECT * FROM {TableName}
                    WHERE EntityType = @entityType AND EntityId = @entityId
                    ORDER BY CreatedAt ASC;";
                command.Parameters.AddWithValue("@entityType", entityType);
                command.Parameters.AddWithValue("@entityId", entityId);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    operations.Add(MapFromReader(reader));
                }

                return operations;
            },
            _retryPolicy,
            _logger,
            $"GetByEntityAsync {entityType}/{entityId}");
    }

    /// <summary>
    /// Gets all operations with a specific correlation ID
    /// </summary>
    public async Task<IEnumerable<OfflineOperation>> GetByCorrelationIdAsync(string correlationId)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                var operations = new List<OfflineOperation>();
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $@"
                    SELECT * FROM {TableName}
                    WHERE CorrelationId = @correlationId
                    ORDER BY CreatedAt ASC;";
                command.Parameters.AddWithValue("@correlationId", correlationId);

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    operations.Add(MapFromReader(reader));
                }

                return operations;
            },
            _retryPolicy,
            _logger,
            $"GetByCorrelationIdAsync {correlationId}");
    }

    /// <summary>
    /// Cancels all pending operations for a specific entity
    /// </summary>
    public async Task CancelByEntityAsync(string entityType, string entityId)
    {
        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = $@"
                        UPDATE {TableName}
                        SET Status = 'Cancelled',
                            CompletedAt = @completedAt
                        WHERE EntityType = @entityType
                          AND EntityId = @entityId
                          AND Status IN ('Pending', 'Processing');";

                    command.Parameters.AddWithValue("@entityType", entityType);
                    command.Parameters.AddWithValue("@entityId", entityId);
                    command.Parameters.AddWithValue("@completedAt", DateTime.UtcNow.ToString("O"));

                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            $"CancelByEntityAsync {entityType}/{entityId}");
    }

    /// <summary>
    /// Resets failed operations to pending status for retry
    /// </summary>
    public async Task<int> ResetFailedAsync()
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    command.CommandText = $@"
                        UPDATE {TableName}
                        SET Status = 'Pending',
                            Attempts = 0,
                            ErrorMessage = NULL
                        WHERE Status = 'Failed';";

                    var affected = await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    _logger?.Info($"Reset {affected} failed operations to pending.", "OfflineQueue");
                    return affected;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            "ResetFailedAsync");
    }

    #region Private Methods

    private async Task UpdateStatusAsync(Guid operationId, OfflineOperationStatus status)
    {
        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;

                    var completedAt = status == OfflineOperationStatus.Completed ? DateTime.UtcNow.ToString("O") : null;

                    command.CommandText = $@"
                        UPDATE {TableName}
                        SET Status = @status,
                            LastAttemptAt = @lastAttemptAt,
                            CompletedAt = @completedAt
                        WHERE Id = @id;";

                    command.Parameters.AddWithValue("@id", operationId.ToString());
                    command.Parameters.AddWithValue("@status", status.ToString());
                    command.Parameters.AddWithValue("@lastAttemptAt", DateTime.UtcNow.ToString("O"));
                    command.Parameters.AddWithValue("@completedAt", (object?)completedAt ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    _logger?.Info($"Operation {operationId} status updated to {status}", "OfflineQueue");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            },
            _retryPolicy,
            _logger,
            $"UpdateStatusAsync {operationId}");
    }

    private void AddOperationParameters(SqliteCommand command, OfflineOperation operation)
    {
        command.Parameters.AddWithValue("@id", operation.Id.ToString());
        command.Parameters.AddWithValue("@operationType", operation.OperationType.ToString());
        command.Parameters.AddWithValue("@entityType", operation.EntityType);
        command.Parameters.AddWithValue("@entityId", operation.EntityId);
        command.Parameters.AddWithValue("@payloadJson", (object?)operation.PayloadJson ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", operation.Status.ToString());
        command.Parameters.AddWithValue("@attempts", operation.Attempts);
        command.Parameters.AddWithValue("@maxAttempts", operation.MaxAttempts);
        command.Parameters.AddWithValue("@createdAt", operation.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@lastAttemptAt",
            operation.LastAttemptAt.HasValue ? operation.LastAttemptAt.Value.ToString("O") : DBNull.Value);
        command.Parameters.AddWithValue("@completedAt",
            operation.CompletedAt.HasValue ? operation.CompletedAt.Value.ToString("O") : DBNull.Value);
        command.Parameters.AddWithValue("@errorMessage", (object?)operation.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@priority", operation.Priority);
        command.Parameters.AddWithValue("@correlationId", (object?)operation.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("@userId",
            operation.UserId.HasValue ? operation.UserId.Value.ToString() : DBNull.Value);
    }

    private OfflineOperation MapFromReader(SqliteDataReader reader)
    {
        return new OfflineOperation
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
            OperationType = Enum.Parse<OfflineOperationType>(reader.GetString(reader.GetOrdinal("OperationType"))),
            EntityType = reader.GetString(reader.GetOrdinal("EntityType")),
            EntityId = reader.GetString(reader.GetOrdinal("EntityId")),
            PayloadJson = GetNullableString(reader, "PayloadJson"),
            Status = Enum.Parse<OfflineOperationStatus>(reader.GetString(reader.GetOrdinal("Status"))),
            Attempts = reader.GetInt32(reader.GetOrdinal("Attempts")),
            MaxAttempts = reader.GetInt32(reader.GetOrdinal("MaxAttempts")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), null,
                System.Globalization.DateTimeStyles.RoundtripKind),
            LastAttemptAt = GetNullableDateTime(reader, "LastAttemptAt"),
            CompletedAt = GetNullableDateTime(reader, "CompletedAt"),
            ErrorMessage = GetNullableString(reader, "ErrorMessage"),
            Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
            CorrelationId = GetNullableString(reader, "CorrelationId"),
            UserId = GetNullableGuid(reader, "UserId")
        };
    }

    private static string? GetNullableString(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetString(ordinal);
        return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static Guid? GetNullableGuid(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        return Guid.Parse(reader.GetString(ordinal));
    }

    #endregion
}

/// <summary>
/// Processor for executing offline queue operations
/// </summary>
public class OfflineQueueProcessor
{
    private readonly OfflineQueue _queue;
    private readonly ILogger? _logger;
    private readonly Dictionary<string, Func<OfflineOperation, CancellationToken, Task<bool>>> _handlers = new();

    /// <summary>
    /// Creates a new queue processor
    /// </summary>
    public OfflineQueueProcessor(OfflineQueue queue, ILogger? logger = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _logger = logger;
    }

    /// <summary>
    /// Registers a handler for a specific entity type
    /// </summary>
    /// <param name="entityType">Entity type name</param>
    /// <param name="handler">Handler function that returns true on success</param>
    public OfflineQueueProcessor RegisterHandler(
        string entityType,
        Func<OfflineOperation, CancellationToken, Task<bool>> handler)
    {
        _handlers[entityType] = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>
    /// Processes all pending operations
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of operations processed successfully</returns>
    public async Task<int> ProcessAsync(CancellationToken ct = default)
    {
        var pending = (await _queue.GetPendingAsync()).ToList();
        if (!pending.Any())
        {
            return 0;
        }

        _logger?.Info($"Processing {pending.Count} offline operations...", "OfflineQueueProcessor");

        int processed = 0;

        foreach (var operation in pending)
        {
            ct.ThrowIfCancellationRequested();

            if (!_handlers.TryGetValue(operation.EntityType, out var handler))
            {
                _logger?.Warning($"No handler registered for entity type '{operation.EntityType}'", null, "OfflineQueueProcessor");
                continue;
            }

            try
            {
                await _queue.MarkProcessingAsync(operation.Id);

                var success = await handler(operation, ct);

                if (success)
                {
                    await _queue.MarkProcessedAsync(operation.Id);
                    processed++;
                }
                else
                {
                    await _queue.MarkFailedAsync(operation.Id, "Handler returned false");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error processing operation {operation.Id}: {ex.Message}", ex, "OfflineQueueProcessor");
                await _queue.MarkFailedAsync(operation.Id, ex.Message);
            }
        }

        _logger?.Info($"Processed {processed}/{pending.Count} operations successfully.", "OfflineQueueProcessor");
        return processed;
    }
}
