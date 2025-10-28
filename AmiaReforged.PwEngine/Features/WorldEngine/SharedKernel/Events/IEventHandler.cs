namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

/// <summary>
/// Defines a handler for a specific domain event type.
/// Event handlers are discovered automatically by the AnvilEventBusService
/// and registered with Anvil's dependency injection container.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event asynchronously.
    /// Event handlers should switch to the main thread if they need to make NWN game calls.
    /// </summary>
    /// <param name="event">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

