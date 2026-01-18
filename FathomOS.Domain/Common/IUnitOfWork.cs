namespace FathomOS.Domain.Common;

/// <summary>
/// Unit of Work pattern interface for managing transactions across multiple repositories.
/// Ensures that changes to multiple aggregates are committed atomically.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Saves all changes made in this unit of work to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Executes the specified action within a transaction.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the action</returns>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the specified action within a transaction.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended Unit of Work with domain event dispatching support.
/// </summary>
public interface IUnitOfWorkWithEvents : IUnitOfWork
{
    /// <summary>
    /// Saves changes and dispatches domain events from modified aggregates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAndDispatchEventsAsync(CancellationToken cancellationToken = default);
}
