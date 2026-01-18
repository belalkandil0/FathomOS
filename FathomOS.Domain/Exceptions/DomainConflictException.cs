namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with the current state of an entity or aggregate.
/// </summary>
public sealed class DomainConflictException : DomainException
{
    /// <inheritdoc />
    public override string ErrorCode => ConflictCode;

    /// <summary>
    /// Gets the specific conflict code.
    /// </summary>
    public string ConflictCode { get; }

    /// <summary>
    /// Gets the type of entity involved in the conflict.
    /// </summary>
    public string? EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity involved in the conflict.
    /// </summary>
    public object? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    public DomainConflictException(string message)
        : base(message)
    {
        ConflictCode = "DOMAIN_CONFLICT";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> class.
    /// </summary>
    /// <param name="conflictCode">The specific conflict code</param>
    /// <param name="message">The error message</param>
    public DomainConflictException(string conflictCode, string message)
        : base(message, new Dictionary<string, object> { ["ConflictCode"] = conflictCode })
    {
        ConflictCode = conflictCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> class.
    /// </summary>
    /// <param name="entityType">The type of entity</param>
    /// <param name="entityId">The entity identifier</param>
    /// <param name="message">The error message</param>
    public DomainConflictException(string entityType, object entityId, string message)
        : base(message, new Dictionary<string, object>
        {
            ["EntityType"] = entityType,
            ["EntityId"] = entityId
        })
    {
        ConflictCode = "DOMAIN_CONFLICT";
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>
    /// Creates a conflict exception for a duplicate entity.
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="identifier">The duplicate identifier</param>
    /// <returns>A new DomainConflictException</returns>
    public static DomainConflictException Duplicate(string entityType, object identifier)
    {
        return new DomainConflictException(
            "DUPLICATE_ENTITY",
            $"An entity of type '{entityType}' with identifier '{identifier}' already exists.");
    }

    /// <summary>
    /// Creates a conflict exception for a duplicate entity.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="identifier">The duplicate identifier</param>
    /// <returns>A new DomainConflictException</returns>
    public static DomainConflictException Duplicate<T>(object identifier)
    {
        return Duplicate(typeof(T).Name, identifier);
    }

    /// <summary>
    /// Creates a conflict exception for an invalid state transition.
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="currentState">The current state</param>
    /// <param name="targetState">The target state</param>
    /// <returns>A new DomainConflictException</returns>
    public static DomainConflictException InvalidStateTransition(string entityType, string currentState, string targetState)
    {
        return new DomainConflictException(
            "INVALID_STATE_TRANSITION",
            $"Cannot transition '{entityType}' from '{currentState}' to '{targetState}'.");
    }
}
