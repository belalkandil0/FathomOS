namespace FathomOS.Core.Interfaces;

/// <summary>
/// Contract for event aggregation (publish/subscribe pattern).
/// Allows decoupled communication between modules and Shell.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to</typeparam>
    /// <param name="handler">The handler to invoke when event is published</param>
    void Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Unsubscribe from events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">The event type to unsubscribe from</typeparam>
    /// <param name="handler">The handler to remove</param>
    void Unsubscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    /// <typeparam name="TEvent">The event type to publish</typeparam>
    /// <param name="eventData">The event data to send</param>
    void Publish<TEvent>(TEvent eventData);
}
