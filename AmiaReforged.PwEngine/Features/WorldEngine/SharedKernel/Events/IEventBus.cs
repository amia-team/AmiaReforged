namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

/// <summary>
/// Event bus for publishing and subscribing to domain events.
/// Phase 3.3 implementation is in-memory and synchronous.
/// Phase 4 will replace with Channel-based async implementation.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// </summary>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Subscribes a handler to events of type TEvent.
    /// </summary>
    /// <param name="handler">Handler function to invoke when event is published</param>
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent;
}

