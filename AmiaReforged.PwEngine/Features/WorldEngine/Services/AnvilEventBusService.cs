using System.Collections.Concurrent;
using System.Reflection;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Services;

/// <summary>
/// Event bus implementation using Anvil's dependency injection pattern.
/// Automatically discovers and registers all event handlers via marker interface.
/// Events are queued and processed asynchronously in the background.
/// </summary>
[ServiceBinding(typeof(IEventBus))]
public class AnvilEventBusService : IEventBus
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ConcurrentQueue<IDomainEvent> _eventQueue = new();
    private readonly Dictionary<Type, List<HandlerInvocation>> _handlers = new();
    private readonly SemaphoreSlim _queueSignal = new(0);
    private bool _isProcessing;

    /// <summary>
    /// Cached handler invocation data for performance optimization.
    /// Stores handler instance, method info, and event type to avoid reflection at runtime.
    /// </summary>
    private class HandlerInvocation
    {
        public object Handler { get; init; }
        public MethodInfo HandleMethod { get; init; }
        public Type EventType { get; init; }

        public HandlerInvocation(object handler, MethodInfo handleMethod, Type eventType)
        {
            Handler = handler;
            HandleMethod = handleMethod;
            EventType = eventType;
        }
    }

    /// <summary>
    /// Initializes the event bus by discovering and caching all event handlers.
    /// Handlers are automatically injected via Anvil's dependency injection using the marker interface pattern.
    /// </summary>
    /// <param name="eventHandlers">All event handlers discovered via IEventHandlerMarker</param>
    public AnvilEventBusService(IEnumerable<IEventHandlerMarker> eventHandlers)
    {
        DiscoverAndCacheHandlers(eventHandlers);
        StartProcessing();
    }

    /// <summary>
    /// Publishes a domain event to the event bus for asynchronous processing.
    /// The event is queued and will be processed by all registered handlers in the background.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event being published</typeparam>
    /// <param name="event">The domain event instance to publish</param>
    /// <param name="cancellationToken">Cancellation token (currently not used in async processing)</param>
    /// <returns>A completed task (events are processed asynchronously in background)</returns>
    /// <example>
    /// <code>
    /// await eventBus.PublishAsync(new OrderCreatedEvent { OrderId = orderId });
    /// </code>
    /// </example>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        _eventQueue.Enqueue(@event);
        _queueSignal.Release();
        Log.Trace($"Published event: {@event.GetType().Name} (ID: {@event.EventId})");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes a handler function to a specific event type.
    /// NOTE: This method is not implemented. Use IEventHandler&lt;TEvent&gt; for automatic registration instead.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to</typeparam>
    /// <param name="handler">The handler function to execute when the event is published</param>
    /// <exception cref="NotImplementedException">Always thrown - use IEventHandler&lt;TEvent&gt; pattern instead</exception>
    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
    {
        throw new NotImplementedException("Use IEventHandler<TEvent> for automatic handler registration");
    }

    private void DiscoverAndCacheHandlers(IEnumerable<IEventHandlerMarker> eventHandlers)
    {
        // Process and cache all handler metadata once
        foreach (IEventHandlerMarker handler in eventHandlers)
        {
            Type handlerType = handler.GetType();
            IEnumerable<Type> handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            foreach (Type handlerInterface in handlerInterfaces)
            {
                Type eventType = handlerInterface.GetGenericArguments()[0];

                // Get the specific HandleAsync method for this event type
                MethodInfo? handleMethod = handlerInterface.GetMethod(nameof(IEventHandler<IDomainEvent>.HandleAsync));

                if (handleMethod == null)
                {
                    Log.Warn($"Could not find HandleAsync method on {handlerType.Name} for {eventType.Name}");
                    continue;
                }

                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<HandlerInvocation>();
                }

                // Cache the handler instance and its method
                HandlerInvocation invocation = new HandlerInvocation(handler, handleMethod, eventType);
                _handlers[eventType].Add(invocation);

                Log.Info($"Registered and cached handler {handlerType.Name} for event {eventType.Name}");
            }
        }

        Log.Info($"Event bus initialized with {_handlers.Count} event types and {_handlers.Values.Sum(h => h.Count)} handlers");
    }

    private void StartProcessing()
    {
        if (_isProcessing) return;

        _isProcessing = true;

        _ = Task.Run(async () =>
        {
            Log.Info("Event bus processing started");

            while (_isProcessing)
            {
                try
                {
                    await _queueSignal.WaitAsync();

                    if (_eventQueue.TryDequeue(out IDomainEvent? @event))
                    {
                        await ProcessEventAsync(@event);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in event bus processing loop");
                }
            }

            Log.Info("Event bus processing stopped");
        });
    }

    private async Task ProcessEventAsync(IDomainEvent @event)
    {
        Type eventType = @event.GetType();

        if (!_handlers.TryGetValue(eventType, out List<HandlerInvocation>? handlerInvocations))
        {
            Log.Trace($"No handlers registered for event {eventType.Name}");
            return;
        }

        Log.Debug($"Processing event {eventType.Name} (ID: {@event.EventId}) with {handlerInvocations.Count} handler(s)");

        foreach (HandlerInvocation invocation in handlerInvocations)
        {
            try
            {
                // Use the pre-cached method info - no reflection lookup needed!

                if (invocation.HandleMethod.Invoke(
                        invocation.Handler,
                        [@event, CancellationToken.None]
                    ) is Task task)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error handling event {eventType.Name} with handler {invocation.Handler.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Stops the event processing loop gracefully.
    /// No new events will be processed after this is called.
    /// </summary>
    public void Stop()
    {
        _isProcessing = false;
        _queueSignal.Release();
    }
}
