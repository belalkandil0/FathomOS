using FathomOS.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FathomOS.Application.Common.Events;

/// <summary>
/// Implementation of <see cref="IDomainEventDispatcher"/> using MediatR.
/// Dispatches domain events as MediatR notifications.
/// </summary>
public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatRDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="publisher">The MediatR publisher</param>
    /// <param name="logger">The logger instance</param>
    public MediatRDomainEventDispatcher(
        IPublisher publisher,
        ILogger<MediatRDomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var eventName = domainEvent.EventType;

        _logger.LogDebug(
            "Dispatching domain event {EventName} ({EventId})",
            eventName, domainEvent.EventId);

        try
        {
            // Wrap the domain event in a notification wrapper
            var notification = new DomainEventNotification<TEvent>(domainEvent);
            await _publisher.Publish(notification, cancellationToken);

            _logger.LogInformation(
                "Domain event {EventName} ({EventId}) dispatched successfully",
                eventName, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error dispatching domain event {EventName} ({EventId})",
                eventName, domainEvent.EventId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var eventList = domainEvents.ToList();

        if (eventList.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Dispatching {Count} domain events", eventList.Count);

        foreach (var domainEvent in eventList)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }

        _logger.LogInformation("Dispatched {Count} domain events successfully", eventList.Count);
    }

    private async Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Use reflection to call the generic dispatch method
        var dispatchMethod = typeof(MediatRDomainEventDispatcher)
            .GetMethod(nameof(DispatchAsync), [typeof(IDomainEvent), typeof(CancellationToken)])?
            .MakeGenericMethod(domainEvent.GetType());

        if (dispatchMethod is not null)
        {
            var task = (Task?)dispatchMethod.Invoke(this, [domainEvent, cancellationToken]);
            if (task is not null)
            {
                await task;
            }
        }
    }
}

/// <summary>
/// MediatR notification wrapper for domain events.
/// Allows domain events to be published through the MediatR pipeline.
/// </summary>
/// <typeparam name="TEvent">The type of domain event</typeparam>
public sealed class DomainEventNotification<TEvent> : INotification
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Gets the domain event.
    /// </summary>
    public TEvent DomainEvent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventNotification{TEvent}"/> class.
    /// </summary>
    /// <param name="domainEvent">The domain event</param>
    public DomainEventNotification(TEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}

/// <summary>
/// Base class for domain event handlers using MediatR.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle</typeparam>
public abstract class DomainEventHandler<TEvent> : INotificationHandler<DomainEventNotification<TEvent>>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event notification.
    /// </summary>
    /// <param name="notification">The notification containing the domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task Handle(DomainEventNotification<TEvent> notification, CancellationToken cancellationToken)
    {
        return HandleAsync(notification.DomainEvent, cancellationToken);
    }

    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected abstract Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
