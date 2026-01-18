namespace FathomOS.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides identity comparison, equality operators, and domain event support.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the entity with the specified identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Protected constructor for ORM frameworks.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>
    /// Raises a domain event for this entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from this entity.
    /// Called after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Determines whether this entity is equal to another entity.
    /// Entities are equal if they have the same type and identifier.
    /// </summary>
    /// <param name="other">The entity to compare with</param>
    /// <returns>True if the entities are equal; otherwise, false</returns>
    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TId>);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Equality operator for entities.
    /// </summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for entities.
    /// </summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Base class for entities using string identifiers.
/// </summary>
public abstract class Entity : Entity<string>
{
    /// <summary>
    /// Initializes a new instance with the specified identifier.
    /// </summary>
    /// <param name="id">The entity identifier</param>
    protected Entity(string id) : base(id)
    {
    }

    /// <summary>
    /// Protected constructor for ORM frameworks.
    /// </summary>
    protected Entity()
    {
    }
}
