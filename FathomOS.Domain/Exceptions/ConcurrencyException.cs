namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict is detected during entity update.
/// </summary>
public sealed class ConcurrencyException : DomainException
{
    /// <inheritdoc />
    public override string ErrorCode => "CONCURRENCY_CONFLICT";

    /// <summary>
    /// Gets the type of entity that had a concurrency conflict.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity that had a concurrency conflict.
    /// </summary>
    public object EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="entityType">The type of entity</param>
    /// <param name="entityId">The entity identifier</param>
    public ConcurrencyException(string entityType, object entityId)
        : base($"A concurrency conflict occurred while updating entity '{entityType}' with identifier '{entityId}'. The entity has been modified by another process.",
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
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="entityId">The entity identifier</param>
    public ConcurrencyException(Type entityType, object entityId)
        : this(entityType.Name, entityId)
    {
    }

    /// <summary>
    /// Creates a ConcurrencyException for a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entityId">The entity identifier</param>
    /// <returns>A new ConcurrencyException instance</returns>
    public static ConcurrencyException For<T>(object entityId)
    {
        return new ConcurrencyException(typeof(T).Name, entityId);
    }
}
