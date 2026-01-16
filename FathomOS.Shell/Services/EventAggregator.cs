using FathomOS.Core.Interfaces;
using System.Collections.Concurrent;

namespace FathomOS.Shell.Services;

/// <summary>
/// Thread-safe implementation of the event aggregator pattern.
/// Allows decoupled communication between modules and Shell.
/// </summary>
public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[eventType] = handlers;
            }

            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <inheritdoc />
    public void Publish<TEvent>(TEvent eventData)
    {
        var eventType = typeof(TEvent);
        List<Delegate>? handlersCopy = null;

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlersCopy = handlers.ToList();
            }
        }

        if (handlersCopy != null)
        {
            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EventAggregator: Handler error for {eventType.Name}: {ex.Message}");
                }
            }
        }
    }
}
