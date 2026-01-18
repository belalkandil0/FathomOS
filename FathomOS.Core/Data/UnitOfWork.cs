// FathomOS.Core/Data/UnitOfWork.cs
// Unit of Work pattern implementation for coordinated database transactions
// Provides transaction management and repository access

using Microsoft.Data.Sqlite;
using FathomOS.Core.Logging;

namespace FathomOS.Core.Data;

/// <summary>
/// Interface for the Unit of Work pattern.
/// Coordinates transactions across multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <returns>Repository instance</returns>
    IGuidRepository<T> GetRepository<T>() where T : class, new();

    /// <summary>
    /// Saves all changes made within the unit of work
    /// </summary>
    /// <returns>Number of affected rows</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync();

    /// <summary>
    /// Gets whether there is an active transaction
    /// </summary>
    bool HasActiveTransaction { get; }
}

/// <summary>
/// SQLite implementation of the Unit of Work pattern.
/// Manages a single connection and transaction for coordinated database operations.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    internal readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger? _logger;
    private readonly RetryPolicy _retryPolicy;
    private readonly Dictionary<Type, object> _repositories = new();
    private readonly Dictionary<string, string> _tableNameMappings = new();

    private SqliteConnection? _connection;
    private SqliteTransaction? _transaction;
    private bool _disposed;
    private int _changeCount;

    /// <summary>
    /// Gets whether there is an active transaction
    /// </summary>
    public bool HasActiveTransaction => _transaction != null;

    /// <summary>
    /// Creates a new Unit of Work instance
    /// </summary>
    /// <param name="connectionFactory">SQLite connection factory</param>
    /// <param name="logger">Optional logger</param>
    /// <param name="retryPolicy">Optional retry policy</param>
    public UnitOfWork(
        SqliteConnectionFactory connectionFactory,
        ILogger? logger = null,
        RetryPolicy? retryPolicy = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.Default;
    }

    /// <summary>
    /// Configures a custom table name mapping for an entity type
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="tableName">Custom table name</param>
    public UnitOfWork WithTableMapping<T>(string tableName) where T : class
    {
        _tableNameMappings[typeof(T).FullName!] = tableName;
        return this;
    }

    /// <inheritdoc/>
    public IGuidRepository<T> GetRepository<T>() where T : class, new()
    {
        var type = typeof(T);

        if (_repositories.TryGetValue(type, out var existing))
        {
            return (IGuidRepository<T>)existing;
        }

        // Get custom table name if configured
        string? tableName = null;
        if (_tableNameMappings.TryGetValue(type.FullName!, out var customTableName))
        {
            tableName = customTableName;
        }

        // Create a new repository that shares the unit of work's connection/transaction
        var repository = new UnitOfWorkRepository<T>(this, tableName, _logger, _retryPolicy);
        _repositories[type] = repository;

        return repository;
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync()
    {
        // If we have an active transaction, this commits it
        if (_transaction != null)
        {
            await CommitTransactionAsync();
        }

        var result = _changeCount;
        _changeCount = 0;
        return result;
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already active. Commit or rollback the current transaction first.");
        }

        await EnsureConnectionAsync();
        _transaction = _connection!.BeginTransaction();
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback.");
        }

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Gets the current connection, creating one if necessary
    /// </summary>
    internal async Task<SqliteConnection> GetConnectionAsync()
    {
        await EnsureConnectionAsync();
        return _connection!;
    }

    /// <summary>
    /// Gets the current transaction (may be null)
    /// </summary>
    internal SqliteTransaction? GetTransaction() => _transaction;

    /// <summary>
    /// Increments the change counter
    /// </summary>
    internal void IncrementChangeCount(int count = 1)
    {
        _changeCount += count;
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection?.Dispose();
            _connection = await _connectionFactory.CreateConnectionAsync();
        }
    }

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _transaction = null;

                _connection?.Dispose();
                _connection = null;

                _repositories.Clear();
            }
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _repositories.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Repository implementation that operates within a Unit of Work context.
/// Shares the connection and transaction with the parent UnitOfWork.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
internal class UnitOfWorkRepository<T> : Repository<T> where T : class, new()
{
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkRepository(
        UnitOfWork unitOfWork,
        string? tableName = null,
        ILogger? logger = null,
        RetryPolicy? retryPolicy = null)
        : base(unitOfWork._connectionFactory, tableName, logger, retryPolicy)
    {
        _unitOfWork = unitOfWork;
    }

    public override async Task<T> AddAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Set timestamps if entity supports auditing
        if (entity is Data.Entities.IAuditableEntity auditable)
        {
            auditable.CreatedAt = DateTime.UtcNow;
        }

        // Generate ID if not set
        EnsureIdIsSetInternal(entity);

        var connection = await _unitOfWork.GetConnectionAsync();
        var transaction = _unitOfWork.GetTransaction();

        if (transaction != null)
        {
            await InsertEntityAsync(connection, transaction, entity);
        }
        else
        {
            await using var localTransaction = connection.BeginTransaction();
            try
            {
                await InsertEntityAsync(connection, localTransaction, entity);
                await localTransaction.CommitAsync();
            }
            catch
            {
                await localTransaction.RollbackAsync();
                throw;
            }
        }

        _unitOfWork.IncrementChangeCount();
        return entity;
    }

    public override async Task UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Set timestamps if entity supports auditing
        if (entity is Data.Entities.IAuditableEntity auditable)
        {
            auditable.ModifiedAt = DateTime.UtcNow;
        }

        var connection = await _unitOfWork.GetConnectionAsync();
        var transaction = _unitOfWork.GetTransaction();

        if (transaction != null)
        {
            await UpdateEntityAsync(connection, transaction, entity);
        }
        else
        {
            await using var localTransaction = connection.BeginTransaction();
            try
            {
                await UpdateEntityAsync(connection, localTransaction, entity);
                await localTransaction.CommitAsync();
            }
            catch
            {
                await localTransaction.RollbackAsync();
                throw;
            }
        }

        _unitOfWork.IncrementChangeCount();
    }

    public override async Task DeleteAsync(Guid id)
    {
        await DeleteByIdUoWAsync(id.ToString());
    }

    public override async Task DeleteAsync(string id)
    {
        await DeleteByIdUoWAsync(id);
    }

    private async Task DeleteByIdUoWAsync(string id)
    {
        var connection = await _unitOfWork.GetConnectionAsync();
        var transaction = _unitOfWork.GetTransaction();

        if (transaction != null)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {_tableName} WHERE {_idColumn} = @id;";
            command.Parameters.AddWithValue("@id", id);
            await command.ExecuteNonQueryAsync();
        }
        else
        {
            await using var localTransaction = connection.BeginTransaction();
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = localTransaction;
                command.CommandText = $"DELETE FROM {_tableName} WHERE {_idColumn} = @id;";
                command.Parameters.AddWithValue("@id", id);
                await command.ExecuteNonQueryAsync();
                await localTransaction.CommitAsync();
            }
            catch
            {
                await localTransaction.RollbackAsync();
                throw;
            }
        }

        _unitOfWork.IncrementChangeCount();
    }

    private void EnsureIdIsSetInternal(T entity)
    {
        var idValue = _idProperty.GetValue(entity);

        if (_idProperty.PropertyType == typeof(Guid))
        {
            if ((Guid)idValue! == Guid.Empty)
            {
                _idProperty.SetValue(entity, Guid.NewGuid());
            }
        }
        else if (_idProperty.PropertyType == typeof(string))
        {
            if (string.IsNullOrEmpty((string?)idValue))
            {
                _idProperty.SetValue(entity, Guid.NewGuid().ToString());
            }
        }
    }
}

/// <summary>
/// Factory for creating Unit of Work instances
/// </summary>
public class UnitOfWorkFactory
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ILogger? _logger;
    private readonly RetryPolicy _retryPolicy;

    /// <summary>
    /// Creates a new Unit of Work factory
    /// </summary>
    /// <param name="connectionFactory">SQLite connection factory</param>
    /// <param name="logger">Optional logger</param>
    /// <param name="retryPolicy">Optional retry policy</param>
    public UnitOfWorkFactory(
        SqliteConnectionFactory connectionFactory,
        ILogger? logger = null,
        RetryPolicy? retryPolicy = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.Default;
    }

    /// <summary>
    /// Creates a new Unit of Work instance
    /// </summary>
    /// <returns>New IUnitOfWork instance</returns>
    public IUnitOfWork Create()
    {
        return new UnitOfWork(_connectionFactory, _logger, _retryPolicy);
    }

    /// <summary>
    /// Creates a new Unit of Work instance with an active transaction
    /// </summary>
    /// <returns>New IUnitOfWork instance with transaction started</returns>
    public async Task<IUnitOfWork> CreateWithTransactionAsync()
    {
        var uow = new UnitOfWork(_connectionFactory, _logger, _retryPolicy);
        await uow.BeginTransactionAsync();
        return uow;
    }
}
