using FathomOS.Domain.Specifications;

namespace FathomOS.Domain.Common;

/// <summary>
/// Generic repository interface for basic CRUD operations on entities.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found; otherwise, null</returns>
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;

    /// <summary>
    /// Gets all entities of this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of all entities</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching the specified specification.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of matching entities</returns>
    Task<IReadOnlyList<T>> GetAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the specification, or null if none found.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or null</returns>
    Task<T?> GetFirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity matching the specification.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching entity</returns>
    /// <exception cref="InvalidOperationException">Thrown when zero or more than one entity matches</exception>
    Task<T> GetSingleAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity matching the specification, or null if none found.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching entity or null</returns>
    /// <exception cref="InvalidOperationException">Thrown when more than one entity matches</exception>
    Task<T?> GetSingleOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specification.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of matching entities</returns>
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all entities of this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The total count of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches; otherwise, false</returns>
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entities exist; otherwise, false</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added entity</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    void Update(T entity);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    /// <param name="entities">The entities to update</param>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove</param>
    void Remove(T entity);

    /// <summary>
    /// Removes multiple entities from the repository.
    /// </summary>
    /// <param name="entities">The entities to remove</param>
    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Repository interface with projection support.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepositoryWithProjection<T> : IRepository<T> where T : class
{
    /// <summary>
    /// Gets projected results matching the specification.
    /// </summary>
    /// <typeparam name="TResult">The projection result type</typeparam>
    /// <param name="specification">The specification with projection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of projected results</returns>
    Task<IReadOnlyList<TResult>> GetAsync<TResult>(
        ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first projected result matching the specification, or default if none found.
    /// </summary>
    /// <typeparam name="TResult">The projection result type</typeparam>
    /// <param name="specification">The specification with projection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching result or default</returns>
    Task<TResult?> GetFirstOrDefaultAsync<TResult>(
        ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for query-only operations.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IReadOnlyRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;

    /// <summary>
    /// Gets all entities of this type.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching the specified specification.
    /// </summary>
    Task<IReadOnlyList<T>> GetAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the specification, or null if none found.
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specification.
    /// </summary>
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification.
    /// </summary>
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
}
