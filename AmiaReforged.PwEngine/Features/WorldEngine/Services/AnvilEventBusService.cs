using System.Collections.Concurrent;
using System.Reflection;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Services;

[ServiceBinding(typeof(IEventBus))]
public class AnvilEventBusService : IEventBus
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ConcurrentQueue<IDomainEvent> _eventQueue = new();
    private readonly Dictionary<Type, List<HandlerInvocation>> _handlers = new();
    private readonly SemaphoreSlim _queueSignal = new(0);
    private bool _isProcessing;

    // Cached handler invocation data
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

    // Inject all event handlers via the marker interface
    public AnvilEventBusService(IEnumerable<IEventHandlerMarker> eventHandlers)
    {
        DiscoverAndCacheHandlers(eventHandlers);
        StartProcessing();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        _eventQueue.Enqueue(@event);
        _queueSignal.Release();
        Log.Trace($"Published event: {@event.GetType().Name} (ID: {@event.EventId})");
        await Task.CompletedTask;
    }

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

    public void Stop()
    {
        _isProcessing = false;
        _queueSignal.Release();
    }
}
