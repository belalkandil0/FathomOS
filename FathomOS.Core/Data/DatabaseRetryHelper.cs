// FathomOS.Core/Data/DatabaseRetryHelper.cs
// Static helper methods for database retry logic with exponential backoff

using Microsoft.Data.Sqlite;
using FathomOS.Core.Logging;

namespace FathomOS.Core.Data;

/// <summary>
/// Static helper class providing retry logic for database operations.
/// Handles transient SQLite errors (SQLITE_BUSY, SQLITE_LOCKED) with exponential backoff and jitter.
/// </summary>
public static class DatabaseRetryHelper
{
    private const string LogSource = "DatabaseRetryHelper";

    /// <summary>
    /// Executes an async database query operation with retry logic.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="policy">Retry policy configuration. Uses default if null.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="operationName">Optional name for logging purposes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="SqliteException">Thrown when all retry attempts are exhausted.</exception>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryPolicy? policy = null,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        policy ??= RetryPolicy.Default;
        policy.Validate();

        var opName = operationName ?? "database operation";
        Exception? lastException = null;

        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (SqliteException ex) when (IsTransientError(ex, policy) && attempt < policy.MaxRetries)
            {
                lastException = ex;
                var delay = policy.CalculateDelay(attempt + 1);

                logger?.Warning(
                    $"Transient SQLite error during {opName} (attempt {attempt + 1}/{policy.MaxRetries + 1}, " +
                    $"error code: {ex.SqliteErrorCode} - {GetErrorDescription(ex.SqliteErrorCode)}). Retrying in {delay}ms.",
                    ex,
                    LogSource);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (SqliteException ex)
            {
                // Non-transient error or last attempt - log and rethrow
                logger?.Error(
                    $"SQLite error during {opName} (error code: {ex.SqliteErrorCode} - {GetErrorDescription(ex.SqliteErrorCode)}). " +
                    (attempt >= policy.MaxRetries ? "Max retries exhausted." : "Non-retryable error."),
                    ex,
                    LogSource);
                throw;
            }
        }

        // Should not reach here, but for safety
        throw lastException ?? new InvalidOperationException("Retry logic completed without result or exception.");
    }

    /// <summary>
    /// Executes an async database command operation with retry logic (no return value).
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="policy">Retry policy configuration. Uses default if null.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="operationName">Optional name for logging purposes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="SqliteException">Thrown when all retry attempts are exhausted.</exception>
    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        RetryPolicy? policy = null,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(
            async () =>
            {
                await operation().ConfigureAwait(false);
                return true; // Dummy return value
            },
            policy,
            logger,
            operationName,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a synchronous database query operation with retry logic.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="policy">Retry policy configuration. Uses default if null.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="operationName">Optional name for logging purposes.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="SqliteException">Thrown when all retry attempts are exhausted.</exception>
    public static T ExecuteWithRetry<T>(
        Func<T> operation,
        RetryPolicy? policy = null,
        ILogger? logger = null,
        string? operationName = null)
    {
        policy ??= RetryPolicy.Default;
        policy.Validate();

        var opName = operationName ?? "database operation";
        Exception? lastException = null;

        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                return operation();
            }
            catch (SqliteException ex) when (IsTransientError(ex, policy) && attempt < policy.MaxRetries)
            {
                lastException = ex;
                var delay = policy.CalculateDelay(attempt + 1);

                logger?.Warning(
                    $"Transient SQLite error during {opName} (attempt {attempt + 1}/{policy.MaxRetries + 1}, " +
                    $"error code: {ex.SqliteErrorCode} - {GetErrorDescription(ex.SqliteErrorCode)}). Retrying in {delay}ms.",
                    ex,
                    LogSource);

                Thread.Sleep(delay);
            }
            catch (SqliteException ex)
            {
                // Non-transient error or last attempt - log and rethrow
                logger?.Error(
                    $"SQLite error during {opName} (error code: {ex.SqliteErrorCode} - {GetErrorDescription(ex.SqliteErrorCode)}). " +
                    (attempt >= policy.MaxRetries ? "Max retries exhausted." : "Non-retryable error."),
                    ex,
                    LogSource);
                throw;
            }
        }

        // Should not reach here, but for safety
        throw lastException ?? new InvalidOperationException("Retry logic completed without result or exception.");
    }

    /// <summary>
    /// Executes a synchronous database command operation with retry logic (no return value).
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="policy">Retry policy configuration. Uses default if null.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="operationName">Optional name for logging purposes.</param>
    /// <exception cref="SqliteException">Thrown when all retry attempts are exhausted.</exception>
    public static void ExecuteWithRetry(
        Action operation,
        RetryPolicy? policy = null,
        ILogger? logger = null,
        string? operationName = null)
    {
        ExecuteWithRetry(
            () =>
            {
                operation();
                return true; // Dummy return value
            },
            policy,
            logger,
            operationName);
    }

    /// <summary>
    /// Wraps a SqliteConnectionFactory transaction execution with retry logic.
    /// </summary>
    /// <param name="connectionFactory">The connection factory to use.</param>
    /// <param name="action">The action to execute within a transaction.</param>
    /// <param name="policy">Retry policy configuration. Uses default if null.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="operationName">Optional name for logging purposes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExecuteInTransactionWithRetryAsync(
        SqliteConnectionFactory connectionFactory,
        Func<SqliteConnection, SqliteTransaction, Task> action,
        RetryPolicy? policy = null,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(
            () => connectionFactory.ExecuteInTransactionAsync(action),
            policy,
            logger,
            operationName,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Wraps a SqliteConnectionFactory transaction execution with retry logic and returns a result.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="connectionFactory">The connection factory to use.</param>
    /// <param name="func">The function to execute within a transaction.</param>
    /// <param name="policy">Retry policy configuration. Uses default if null.</param>
    /// <param name="logger">Optional logger for retry attempts.</param>
    /// <param name="operationName">Optional name for logging purposes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the function.</returns>
    public static async Task<T> ExecuteInTransactionWithRetryAsync<T>(
        SqliteConnectionFactory connectionFactory,
        Func<SqliteConnection, SqliteTransaction, Task<T>> func,
        RetryPolicy? policy = null,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            () => connectionFactory.ExecuteInTransactionAsync(func),
            policy,
            logger,
            operationName,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines if a SQLite exception represents a transient error that should be retried.
    /// By default, handles SQLITE_BUSY (5) and SQLITE_LOCKED (6).
    /// </summary>
    /// <param name="exception">The SQLite exception to check.</param>
    /// <param name="policy">The retry policy containing retryable error codes.</param>
    /// <returns>True if the error is transient and should be retried.</returns>
    public static bool IsTransientError(SqliteException exception, RetryPolicy? policy = null)
    {
        if (exception == null)
            return false;

        policy ??= RetryPolicy.Default;
        return policy.RetryableSqliteErrorCodes.Contains(exception.SqliteErrorCode);
    }

    /// <summary>
    /// Gets a human-readable description of a SQLite error code.
    /// </summary>
    /// <param name="errorCode">The SQLite error code.</param>
    /// <returns>A description of the error code.</returns>
    public static string GetErrorDescription(int errorCode)
    {
        return errorCode switch
        {
            5 => "SQLITE_BUSY - The database file is locked",
            6 => "SQLITE_LOCKED - A table in the database is locked",
            10 => "SQLITE_IOERR - Some kind of disk I/O error occurred",
            11 => "SQLITE_CORRUPT - The database disk image is malformed",
            13 => "SQLITE_FULL - Database or disk is full",
            14 => "SQLITE_CANTOPEN - Unable to open database file",
            17 => "SQLITE_SCHEMA - Database schema has changed",
            19 => "SQLITE_CONSTRAINT - Constraint violation",
            _ => $"SQLite error code: {errorCode}"
        };
    }
}

/// <summary>
/// Result wrapper for retry operations that provides additional context about the execution.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public sealed class RetryResult<T>
{
    /// <summary>
    /// Gets the result value if the operation succeeded.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the number of attempts made before success or failure.
    /// </summary>
    public int AttemptsUsed { get; init; }

    /// <summary>
    /// Gets the total time spent including delays.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the exception if the operation failed, null otherwise.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static RetryResult<T> Success(T value, int attempts, TimeSpan duration)
    {
        return new RetryResult<T>
        {
            Value = value,
            IsSuccess = true,
            AttemptsUsed = attempts,
            TotalDuration = duration,
            Exception = null
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static RetryResult<T> Failure(Exception exception, int attempts, TimeSpan duration)
    {
        return new RetryResult<T>
        {
            Value = default,
            IsSuccess = false,
            AttemptsUsed = attempts,
            TotalDuration = duration,
            Exception = exception
        };
    }
}
