using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

/// <summary>
/// In-memory, synchronous event bus for Phase 3.3.
/// Events are dispatched immediately to all subscribers on the calling thread.
/// This will be replaced with a Channel-based async implementation in Phase 4.
/// </summary>
[ServiceBinding(typeof(IEventBus))]
public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly List<IDomainEvent> _publishedEvents = new();
    private readonly object _lock = new();

    /// <summary>
    /// Published events (for testing/debugging).
    /// </summary>
    public IReadOnlyList<IDomainEvent> PublishedEvents
    {
        get
        {
            lock (_lock)
            {
                return _publishedEvents.ToList();
            }
        }
    }

    /// <summary>
    /// Clear all published events (for testing).
    /// </summary>
    public void ClearPublishedEvents()
    {
        lock (_lock)
        {
            _publishedEvents.Clear();
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        List<Func<TEvent, CancellationToken, Task>> handlers;

        lock (_lock)
        {
            _publishedEvents.Add(@event);

            if (!_subscribers.TryGetValue(typeof(TEvent), out var subscriberList))
            {
                return; // No subscribers
            }

            handlers = subscriberList.Cast<Func<TEvent, CancellationToken, Task>>().ToList();
        }

        // Execute handlers outside the lock (synchronously for Phase 3.3)
        foreach (var handler in handlers)
        {
            await handler(@event, cancellationToken);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var subscriberList))
            {
                subscriberList = new List<Delegate>();
                _subscribers[typeof(TEvent)] = subscriberList;
            }

            subscriberList.Add(handler);
        }
    }
}

