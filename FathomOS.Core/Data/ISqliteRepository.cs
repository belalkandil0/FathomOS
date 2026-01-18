// FathomOS.Core/Data/ISqliteRepository.cs
// Base repository interface for SQLite data access
// All repositories should extend this interface for consistent CRUD operations

namespace FathomOS.Core.Data;

#region String ID Interfaces (Legacy/Backward Compatible)

/// <summary>
/// Base repository interface for entity data access with string identifiers.
/// Provides standard CRUD operations for all entity types.
/// Used for backward compatibility with existing implementations (e.g., Certificate).
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its unique identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Gets all entities of this type
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <returns>The added entity</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="entity">The entity with updated values</param>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity by its identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    Task DeleteAsync(string id);

    /// <summary>
    /// Checks if an entity with the given identifier exists
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>True if entity exists, false otherwise</returns>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Gets the count of all entities
    /// </summary>
    /// <returns>Total entity count</returns>
    Task<int> CountAsync();
}

/// <summary>
/// Extended repository interface with batch operations
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IBatchRepository<T> : IRepository<T> where T : class
{
    /// <summary>
    /// Adds multiple entities in a single transaction
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <returns>The added entities</returns>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Updates multiple entities in a single transaction
    /// </summary>
    /// <param name="entities">The entities to update</param>
    Task UpdateRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Deletes multiple entities by their identifiers
    /// </summary>
    /// <param name="ids">The entity identifiers</param>
    Task DeleteRangeAsync(IEnumerable<string> ids);
}

/// <summary>
/// Repository interface with sync support for offline-first pattern
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface ISyncableRepository<T> : IBatchRepository<T> where T : class
{
    /// <summary>
    /// Gets all entities pending synchronization
    /// </summary>
    /// <returns>Entities with pending sync status</returns>
    Task<IEnumerable<T>> GetPendingSyncAsync();

    /// <summary>
    /// Marks an entity as synced to the server
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="syncedAt">When the sync occurred (UTC)</param>
    Task MarkSyncedAsync(string id, DateTime? syncedAt = null);

    /// <summary>
    /// Marks multiple entities as synced
    /// </summary>
    /// <param name="ids">The entity identifiers</param>
    /// <param name="syncedAt">When the sync occurred (UTC)</param>
    Task MarkSyncedRangeAsync(IEnumerable<string> ids, DateTime? syncedAt = null);

    /// <summary>
    /// Marks an entity sync as failed
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="errorMessage">The error message</param>
    Task MarkSyncFailedAsync(string id, string? errorMessage = null);

    /// <summary>
    /// Gets the count of entities pending sync
    /// </summary>
    /// <returns>Count of pending entities</returns>
    Task<int> GetPendingSyncCountAsync();
}

#endregion

#region Guid ID Interfaces (DATABASE-AGENT Spec)

/// <summary>
/// Base repository interface for entity data access with Guid identifiers.
/// This is the primary interface matching the DATABASE-AGENT specification.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IGuidRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its unique Guid identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all entities of this type
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <returns>The added entity</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="entity">The entity with updated values</param>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity by its Guid identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Checks if an entity with the given Guid identifier exists
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <returns>True if entity exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id);
}

/// <summary>
/// Extended repository interface with batch operations using Guid identifiers.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IGuidBatchRepository<T> : IGuidRepository<T> where T : class
{
    /// <summary>
    /// Adds multiple entities in a single transaction
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <returns>The added entities</returns>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Updates multiple entities in a single transaction
    /// </summary>
    /// <param name="entities">The entities to update</param>
    Task UpdateRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Deletes multiple entities by their Guid identifiers
    /// </summary>
    /// <param name="ids">The entity identifiers</param>
    Task DeleteRangeAsync(IEnumerable<Guid> ids);
}

/// <summary>
/// Repository interface with sync support for offline-first pattern using Guid identifiers.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IGuidSyncableRepository<T> : IGuidBatchRepository<T> where T : class
{
    /// <summary>
    /// Gets all entities pending synchronization
    /// </summary>
    /// <returns>Entities with pending sync status</returns>
    Task<IEnumerable<T>> GetPendingSyncAsync();

    /// <summary>
    /// Marks an entity as synced to the server
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="syncedAt">When the sync occurred (UTC)</param>
    Task MarkSyncedAsync(Guid id, DateTime? syncedAt = null);

    /// <summary>
    /// Marks multiple entities as synced
    /// </summary>
    /// <param name="ids">The entity identifiers</param>
    /// <param name="syncedAt">When the sync occurred (UTC)</param>
    Task MarkSyncedRangeAsync(IEnumerable<Guid> ids, DateTime? syncedAt = null);

    /// <summary>
    /// Marks an entity sync as failed
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="errorMessage">The error message</param>
    Task MarkSyncFailedAsync(Guid id, string? errorMessage = null);

    /// <summary>
    /// Gets the count of entities pending sync
    /// </summary>
    /// <returns>Count of pending entities</returns>
    Task<int> GetPendingSyncCountAsync();
}

#endregion
