namespace FathomOS.Domain.Common;

/// <summary>
/// Base class for aggregate roots in the domain model.
/// Aggregate roots are the primary entities that encapsulate a cluster of related entities
/// and value objects, enforcing invariants across the aggregate boundary.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the version number for optimistic concurrency control.
    /// Incremented each time the aggregate is persisted.
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Gets the timestamp when this aggregate was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; protected init; }

    /// <summary>
    /// Gets or sets the timestamp when this aggregate was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; protected set; }

    /// <summary>
    /// Gets the identifier of the user who created this aggregate.
    /// </summary>
    public string? CreatedBy { get; protected init; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified this aggregate.
    /// </summary>
    public string? ModifiedBy { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the aggregate root with the specified identifier.
    /// </summary>
    /// <param name="id">The aggregate root identifier</param>
    protected AggregateRoot(TId id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
        Version = 0;
    }

    /// <summary>
    /// Protected constructor for ORM frameworks.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Updates the modification tracking fields.
    /// </summary>
    /// <param name="modifiedBy">The identifier of the user making the modification</param>
    protected void SetModified(string? modifiedBy = null)
    {
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Increments the version number for optimistic concurrency.
    /// Called by the persistence layer when saving changes.
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
        ModifiedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Base class for aggregate roots using string identifiers.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<string>
{
    /// <summary>
    /// Initializes a new instance with the specified identifier.
    /// </summary>
    /// <param name="id">The aggregate root identifier</param>
    protected AggregateRoot(string id) : base(id)
    {
    }

    /// <summary>
    /// Protected constructor for ORM frameworks.
    /// </summary>
    protected AggregateRoot()
    {
    }
}
