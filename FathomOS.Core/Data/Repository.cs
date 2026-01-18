// FathomOS.Core/Data/Repository.cs
// Generic repository implementation with SQLite backend
// Implements IRepository<T> with full CRUD support and retry logic

using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Text;
using FathomOS.Core.Data.Entities;
using FathomOS.Core.Logging;

namespace FathomOS.Core.Data;

/// <summary>
/// Generic SQLite repository implementation providing CRUD operations for entities.
/// Supports entities with Guid or string primary keys.
/// Implements IGuidRepository for Guid-based IDs (DATABASE-AGENT spec).
/// </summary>
/// <typeparam name="T">The entity type (must be a class)</typeparam>
public class Repository<T> : IGuidRepository<T> where T : class, new()
{
    protected readonly SqliteConnectionFactory _connectionFactory;
    protected readonly ILogger? _logger;
    protected readonly RetryPolicy _retryPolicy;
    protected readonly string _tableName;
    protected readonly string _idColumn;
    protected readonly PropertyInfo _idProperty;
    protected readonly PropertyInfo[] _properties;

    /// <summary>
    /// Creates a new repository instance
    /// </summary>
    /// <param name="connectionFactory">SQLite connection factory</param>
    /// <param name="tableName">Optional table name (defaults to type name + 's')</param>
    /// <param name="logger">Optional logger for retry and error logging</param>
    /// <param name="retryPolicy">Optional retry policy. Uses default if null.</param>
    public Repository(
        SqliteConnectionFactory connectionFactory,
        string? tableName = null,
        ILogger? logger = null,
        RetryPolicy? retryPolicy = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
        _retryPolicy = retryPolicy ?? RetryPolicy.Default;

        // Determine table name (convention: pluralize type name or use provided)
        _tableName = tableName ?? typeof(T).Name + "s";

        // Find the ID property (supports Id, {TypeName}Id, or [Key] attribute)
        _idProperty = FindIdProperty() ?? throw new InvalidOperationException(
            $"Entity type {typeof(T).Name} must have an Id, {typeof(T).Name}Id property, or a property marked with [Key] attribute.");

        _idColumn = _idProperty.Name;

        // Get all writable properties for mapping
        _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();
    }

    #region IGuidRepository<T> Implementation

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await GetByIdInternalAsync(id.ToString());
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                var entities = new List<T>();
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT * FROM {_tableName};";

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    entities.Add(MapFromReader(reader));
                }

                return entities;
            },
            _retryPolicy,
            _logger,
            $"GetAllAsync<{typeof(T).Name}>");
    }

    /// <inheritdoc/>
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Set timestamps if entity supports auditing
        if (entity is IAuditableEntity auditable)
        {
            auditable.CreatedAt = DateTime.UtcNow;
        }

        // Generate ID if not set
        EnsureIdIsSet(entity);

        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await InsertEntityAsync(connection, transaction, entity);
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
            $"AddAsync<{typeof(T).Name}>");

        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Set timestamps if entity supports auditing
        if (entity is IAuditableEntity auditable)
        {
            auditable.ModifiedAt = DateTime.UtcNow;
        }

        await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var transaction = connection.BeginTransaction();

                try
                {
                    await UpdateEntityAsync(connection, transaction, entity);
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
            $"UpdateAsync<{typeof(T).Name}>");
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(Guid id)
    {
        await DeleteByIdInternalAsync(id.ToString());
    }

    /// <summary>
    /// Deletes an entity by its string identifier
    /// </summary>
    public virtual async Task DeleteAsync(string id)
    {
        await DeleteByIdInternalAsync(id);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await ExistsInternalAsync(id.ToString());
    }

    /// <summary>
    /// Checks if an entity with the given string identifier exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(string id)
    {
        return await ExistsInternalAsync(id);
    }

    #endregion

    #region Additional Query Methods

    /// <summary>
    /// Gets the count of all entities
    /// </summary>
    public virtual async Task<int> CountAsync()
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT COUNT(*) FROM {_tableName};";
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            },
            _retryPolicy,
            _logger,
            $"CountAsync<{typeof(T).Name}>");
    }

    /// <summary>
    /// Queries entities with a custom WHERE clause
    /// </summary>
    /// <param name="whereClause">SQL WHERE clause without the WHERE keyword</param>
    /// <param name="parameters">Query parameters</param>
    public virtual async Task<IEnumerable<T>> QueryAsync(string whereClause, params (string Name, object? Value)[] parameters)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                var entities = new List<T>();
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT * FROM {_tableName} WHERE {whereClause};";
                foreach (var (name, value) in parameters)
                {
                    command.Parameters.AddWithValue(name, value ?? DBNull.Value);
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    entities.Add(MapFromReader(reader));
                }

                return entities;
            },
            _retryPolicy,
            _logger,
            $"QueryAsync<{typeof(T).Name}>");
    }

    /// <summary>
    /// Gets the first entity matching a predicate, or null
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultAsync(string whereClause, params (string Name, object? Value)[] parameters)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT * FROM {_tableName} WHERE {whereClause} LIMIT 1;";
                foreach (var (name, value) in parameters)
                {
                    command.Parameters.AddWithValue(name, value ?? DBNull.Value);
                }

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapFromReader(reader);
                }
                return null;
            },
            _retryPolicy,
            _logger,
            $"FirstOrDefaultAsync<{typeof(T).Name}>");
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Adds multiple entities in a single transaction
    /// </summary>
    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities?.ToList() ?? throw new ArgumentNullException(nameof(entities));
        if (!entityList.Any())
            return entityList;

        var now = DateTime.UtcNow;
        foreach (var entity in entityList)
        {
            if (entity is IAuditableEntity auditable)
            {
                auditable.CreatedAt = now;
            }
            EnsureIdIsSet(entity);
        }

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var entity in entityList)
            {
                await InsertEntityAsync(connection, transaction, entity);
            }
        });

        return entityList;
    }

    /// <summary>
    /// Updates multiple entities in a single transaction
    /// </summary>
    public virtual async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities?.ToList() ?? throw new ArgumentNullException(nameof(entities));
        if (!entityList.Any())
            return;

        var now = DateTime.UtcNow;
        foreach (var entity in entityList)
        {
            if (entity is IAuditableEntity auditable)
            {
                auditable.ModifiedAt = now;
            }
        }

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var entity in entityList)
            {
                await UpdateEntityAsync(connection, transaction, entity);
            }
        });
    }

    /// <summary>
    /// Deletes multiple entities by their identifiers
    /// </summary>
    public virtual async Task DeleteRangeAsync(IEnumerable<Guid> ids)
    {
        await DeleteRangeByIdsInternalAsync(ids.Select(id => id.ToString()));
    }

    /// <summary>
    /// Deletes multiple entities by their string identifiers
    /// </summary>
    public virtual async Task DeleteRangeAsync(IEnumerable<string> ids)
    {
        await DeleteRangeByIdsInternalAsync(ids);
    }

    #endregion

    #region Protected Helper Methods

    protected virtual async Task<T?> GetByIdInternalAsync(string id)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT * FROM {_tableName} WHERE {_idColumn} = @id;";
                command.Parameters.AddWithValue("@id", id);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapFromReader(reader);
                }
                return null;
            },
            _retryPolicy,
            _logger,
            $"GetByIdAsync<{typeof(T).Name}>");
    }

    protected virtual async Task DeleteByIdInternalAsync(string id)
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
                    command.CommandText = $"DELETE FROM {_tableName} WHERE {_idColumn} = @id;";
                    command.Parameters.AddWithValue("@id", id);

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
            $"DeleteAsync<{typeof(T).Name}>");
    }

    protected virtual async Task<bool> ExistsInternalAsync(string id)
    {
        return await DatabaseRetryHelper.ExecuteWithRetryAsync(
            async () =>
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                await using var command = connection.CreateCommand();

                command.CommandText = $"SELECT COUNT(1) FROM {_tableName} WHERE {_idColumn} = @id;";
                command.Parameters.AddWithValue("@id", id);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            },
            _retryPolicy,
            _logger,
            $"ExistsAsync<{typeof(T).Name}>");
    }

    protected virtual async Task DeleteRangeByIdsInternalAsync(IEnumerable<string> ids)
    {
        var idList = ids?.ToList() ?? throw new ArgumentNullException(nameof(ids));
        if (!idList.Any())
            return;

        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var id in idList)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"DELETE FROM {_tableName} WHERE {_idColumn} = @id;";
                command.Parameters.AddWithValue("@id", id);
                await command.ExecuteNonQueryAsync();
            }
        });
    }

    protected virtual async Task InsertEntityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        T entity)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        var columns = new StringBuilder();
        var values = new StringBuilder();
        var first = true;

        foreach (var prop in _properties)
        {
            if (!first)
            {
                columns.Append(", ");
                values.Append(", ");
            }
            first = false;

            columns.Append(prop.Name);
            values.Append($"@{prop.Name}");
            command.Parameters.AddWithValue($"@{prop.Name}", GetParameterValue(prop, entity));
        }

        command.CommandText = $"INSERT INTO {_tableName} ({columns}) VALUES ({values});";
        await command.ExecuteNonQueryAsync();
    }

    protected virtual async Task UpdateEntityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        T entity)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        var setClause = new StringBuilder();
        var first = true;

        foreach (var prop in _properties)
        {
            // Skip the ID column in the SET clause
            if (prop.Name == _idColumn)
            {
                command.Parameters.AddWithValue($"@{prop.Name}", GetParameterValue(prop, entity));
                continue;
            }

            if (!first)
            {
                setClause.Append(", ");
            }
            first = false;

            setClause.Append($"{prop.Name} = @{prop.Name}");
            command.Parameters.AddWithValue($"@{prop.Name}", GetParameterValue(prop, entity));
        }

        command.CommandText = $"UPDATE {_tableName} SET {setClause} WHERE {_idColumn} = @{_idColumn};";
        await command.ExecuteNonQueryAsync();
    }

    protected virtual T MapFromReader(SqliteDataReader reader)
    {
        var entity = new T();

        foreach (var prop in _properties)
        {
            try
            {
                var ordinal = reader.GetOrdinal(prop.Name);
                if (!reader.IsDBNull(ordinal))
                {
                    var value = reader.GetValue(ordinal);
                    var convertedValue = ConvertFromDatabase(value, prop.PropertyType);
                    prop.SetValue(entity, convertedValue);
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Column doesn't exist in result set - skip
            }
        }

        return entity;
    }

    protected virtual object GetParameterValue(PropertyInfo property, T entity)
    {
        var value = property.GetValue(entity);
        return ConvertToDatabase(value, property.PropertyType);
    }

    protected virtual object ConvertToDatabase(object? value, Type propertyType)
    {
        if (value == null)
            return DBNull.Value;

        // Handle Guid
        if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
        {
            return value.ToString()!;
        }

        // Handle DateTime (store as ISO 8601)
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            return ((DateTime)value).ToString("O");
        }

        // Handle enums
        if (propertyType.IsEnum || (Nullable.GetUnderlyingType(propertyType)?.IsEnum ?? false))
        {
            return value.ToString()!;
        }

        // Handle boolean
        if (propertyType == typeof(bool) || propertyType == typeof(bool?))
        {
            return (bool)value ? 1 : 0;
        }

        return value;
    }

    protected virtual object? ConvertFromDatabase(object value, Type propertyType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // Handle Guid
        if (underlyingType == typeof(Guid))
        {
            return Guid.Parse(value.ToString()!);
        }

        // Handle DateTime
        if (underlyingType == typeof(DateTime))
        {
            return DateTime.Parse(value.ToString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        // Handle enums
        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, value.ToString()!);
        }

        // Handle boolean
        if (underlyingType == typeof(bool))
        {
            if (value is long l)
                return l != 0;
            if (value is int i)
                return i != 0;
            return Convert.ToBoolean(value);
        }

        return Convert.ChangeType(value, underlyingType);
    }

    private PropertyInfo? FindIdProperty()
    {
        // First, look for property with [Key] attribute
        var keyProperty = typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttributes()
                .Any(a => a.GetType().Name == "KeyAttribute"));
        if (keyProperty != null)
            return keyProperty;

        // Then look for "Id" property
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
            return idProperty;

        // Finally, look for "{TypeName}Id" property
        return typeof(T).GetProperty($"{typeof(T).Name}Id");
    }

    private void EnsureIdIsSet(T entity)
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

    #endregion
}
