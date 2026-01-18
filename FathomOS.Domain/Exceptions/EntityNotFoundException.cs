namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found in the domain.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    /// <inheritdoc />
    public override string ErrorCode => "ENTITY_NOT_FOUND";

    /// <summary>
    /// Gets the type of entity that was not found.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public object EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityType">The type of entity</param>
    /// <param name="entityId">The entity identifier</param>
    public EntityNotFoundException(string entityType, object entityId)
        : base($"Entity '{entityType}' with identifier '{entityId}' was not found.",
            new Dictionary<string, object>
            {
                ["EntityType"] = entityType,
                ["EntityId"] = entityId
            })
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity identifier</param>
    public EntityNotFoundException(Type entityType, object entityId)
        : this(entityType.Name, entityId)
    {
    }

    /// <summary>
    /// Creates an EntityNotFoundException for a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityId">The entity identifier</param>
    /// <returns>A new EntityNotFoundException instance</returns>
    public static EntityNotFoundException For<T>(object entityId)
    {
        return new EntityNotFoundException(typeof(T).Name, entityId);
    }
}
