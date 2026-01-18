namespace FathomOS.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred (UTC).
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Gets the type name of the event for serialization/logging purposes.
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Base class for domain events providing common functionality.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string EventType => GetType().Name;
}

/// <summary>
/// Handler interface for domain events.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dispatcher interface for publishing domain events to their handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a single domain event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event</typeparam>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Dispatches multiple domain events to their handlers.
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
