using System.Collections.Concurrent;
using FathomOS.Core.Interfaces;

namespace FathomOS.Core.Messaging;

/// <summary>
/// Thread-safe implementation of the event aggregator pattern using weak references
/// to prevent memory leaks from forgotten unsubscriptions.
/// </summary>
/// <remarks>
/// The EventAggregator enables loose coupling between publishers and subscribers.
/// Publishers don't need to know about subscribers, and subscribers automatically
/// get cleaned up when garbage collected due to weak reference usage.
/// </remarks>
public class EventAggregator : IEventAggregator
{
    /// <summary>
    /// Stores subscriptions by event type. Uses WeakReference to prevent memory leaks.
    /// </summary>
    private readonly ConcurrentDictionary<Type, List<WeakSubscription>> _subscriptions = new();

    /// <summary>
    /// Lock object for subscription list modifications.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Counter for cleanup frequency - cleanup dead references every N publishes.
    /// </summary>
    private int _publishCount;
    private const int CleanupFrequency = 100;

    /// <inheritdoc/>
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        var eventType = typeof(TEvent);
        var subscription = new WeakSubscription(handler, handler.Target);

        _subscriptions.AddOrUpdate(
            eventType,
            _ => new List<WeakSubscription> { subscription },
            (_, existing) =>
            {
                lock (_lock)
                {
                    // Don't add duplicate subscriptions
                    if (!existing.Any(s => s.IsSameHandler(handler)))
                    {
                        existing.Add(subscription);
                    }
                    return existing;
                }
            });
    }

    /// <inheritdoc/>
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        var eventType = typeof(TEvent);

        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            lock (_lock)
            {
                subscriptions.RemoveAll(s => s.IsSameHandler(handler) || !s.IsAlive);
            }
        }
    }

    /// <inheritdoc/>
    public void Publish<TEvent>(TEvent eventData)
    {
        var eventType = typeof(TEvent);

        // Periodic cleanup of dead references
        if (Interlocked.Increment(ref _publishCount) % CleanupFrequency == 0)
        {
            CleanupDeadReferences();
        }

        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            return;
        }

        // Get a snapshot of live handlers to invoke
        List<Action<TEvent>> handlersToInvoke;
        lock (_lock)
        {
            handlersToInvoke = subscriptions
                .Where(s => s.IsAlive)
                .Select(s => s.GetHandler<TEvent>())
                .Where(h => h != null)
                .ToList()!;
        }

        // Invoke handlers outside of lock
        foreach (var handler in handlersToInvoke)
        {
            try
            {
                handler(eventData);
            }
            catch (Exception ex)
            {
                // Log but don't propagate exceptions from handlers
                System.Diagnostics.Debug.WriteLine(
                    $"EventAggregator: Handler for {eventType.Name} threw exception: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Checks if there are any active subscribers for a given event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to check.</typeparam>
    /// <returns>True if there are active subscribers.</returns>
    public bool HasSubscribers<TEvent>()
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            lock (_lock)
            {
                return subscriptions.Any(s => s.IsAlive);
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the number of active subscribers for a given event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to count.</typeparam>
    /// <returns>The number of active subscribers.</returns>
    public int GetSubscriberCount<TEvent>()
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            lock (_lock)
            {
                return subscriptions.Count(s => s.IsAlive);
            }
        }
        return 0;
    }

    /// <summary>
    /// Removes all subscriptions. Useful for cleanup during shutdown.
    /// </summary>
    public void ClearAllSubscriptions()
    {
        _subscriptions.Clear();
    }

    /// <summary>
    /// Cleans up dead (garbage collected) references from all subscription lists.
    /// Called periodically during publish operations.
    /// </summary>
    private void CleanupDeadReferences()
    {
        foreach (var kvp in _subscriptions)
        {
            lock (_lock)
            {
                kvp.Value.RemoveAll(s => !s.IsAlive);
            }
        }

        // Remove event types with no subscribers
        var emptyTypes = _subscriptions
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var type in emptyTypes)
        {
            _subscriptions.TryRemove(type, out _);
        }
    }

    /// <summary>
    /// Represents a weak subscription to an event.
    /// Uses weak references to prevent memory leaks.
    /// </summary>
    private sealed class WeakSubscription
    {
        private readonly WeakReference? _targetReference;
        private readonly Delegate _handler;
        private readonly bool _isStaticHandler;

        /// <summary>
        /// Creates a new weak subscription.
        /// </summary>
        /// <param name="handler">The event handler delegate.</param>
        /// <param name="target">The target object (null for static methods).</param>
        public WeakSubscription(Delegate handler, object? target)
        {
            _handler = handler;
            _isStaticHandler = target == null;
            _targetReference = target != null ? new WeakReference(target) : null;
        }

        /// <summary>
        /// Gets whether the subscription is still alive (target hasn't been GC'd).
        /// Static handlers are always considered alive.
        /// </summary>
        public bool IsAlive => _isStaticHandler || (_targetReference?.IsAlive ?? false);

        /// <summary>
        /// Gets the handler if the subscription is still alive.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <returns>The handler, or null if the target has been collected.</returns>
        public Action<TEvent>? GetHandler<TEvent>()
        {
            if (!IsAlive)
                return null;

            return _handler as Action<TEvent>;
        }

        /// <summary>
        /// Checks if this subscription is for the same handler.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="handler">The handler to compare.</param>
        /// <returns>True if this is the same handler.</returns>
        public bool IsSameHandler<TEvent>(Action<TEvent> handler)
        {
            if (_handler is not Action<TEvent> thisHandler)
                return false;

            // For static handlers, compare delegates directly
            if (_isStaticHandler)
                return thisHandler == handler;

            // For instance handlers, compare target and method
            var target = _targetReference?.Target;
            return target != null &&
                   ReferenceEquals(target, handler.Target) &&
                   thisHandler.Method == handler.Method;
        }
    }
}

/// <summary>
/// Extension methods for IEventAggregator to support additional subscription patterns.
/// </summary>
public static class EventAggregatorExtensions
{
    /// <summary>
    /// Subscribes to an event and returns a token that can be used to unsubscribe.
    /// Useful for lambda handlers that can't be easily unsubscribed.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="aggregator">The event aggregator.</param>
    /// <param name="handler">The event handler.</param>
    /// <returns>A subscription token that can be disposed to unsubscribe.</returns>
    public static IDisposable SubscribeWithToken<TEvent>(
        this IEventAggregator aggregator,
        Action<TEvent> handler)
    {
        aggregator.Subscribe(handler);
        return new SubscriptionToken<TEvent>(aggregator, handler);
    }

    /// <summary>
    /// Subscribes to an event and automatically unsubscribes after the first occurrence.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="aggregator">The event aggregator.</param>
    /// <param name="handler">The event handler.</param>
    public static void SubscribeOnce<TEvent>(
        this IEventAggregator aggregator,
        Action<TEvent> handler)
    {
        Action<TEvent>? wrapper = null;
        wrapper = (e) =>
        {
            aggregator.Unsubscribe(wrapper!);
            handler(e);
        };
        aggregator.Subscribe(wrapper);
    }

    /// <summary>
    /// Token that allows disposing of an event subscription.
    /// </summary>
    private sealed class SubscriptionToken<TEvent> : IDisposable
    {
        private readonly IEventAggregator _aggregator;
        private readonly Action<TEvent> _handler;
        private bool _disposed;

        public SubscriptionToken(IEventAggregator aggregator, Action<TEvent> handler)
        {
            _aggregator = aggregator;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _aggregator.Unsubscribe(_handler);
        }
    }
}
