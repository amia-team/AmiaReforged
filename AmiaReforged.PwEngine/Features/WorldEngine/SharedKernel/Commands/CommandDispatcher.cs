using System.Collections.Concurrent;
using System.Reflection;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using LightInject;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Central command dispatcher implementation.
/// Routes commands to their handlers via Anvil DI auto-discovery and publishes domain events.
/// </summary>
[ServiceBinding(typeof(ICommandDispatcher))]
public sealed class CommandDispatcher : ICommandDispatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ServiceContainer? _serviceContainer;
    private readonly Lazy<IEventBus> _eventBus;
    private readonly ConcurrentDictionary<Type, HandlerInvocation> _handlerCache = new();

    private sealed class HandlerInvocation(
        Type? serviceType,
        string? serviceName,
        object? handler,
        Type handlerType,
        MethodInfo handleMethod,
        Type commandType)
    {
        public Type? ServiceType { get; } = serviceType;
        public string? ServiceName { get; } = serviceName;
        public object? Handler { get; } = handler;
        public Type HandlerType { get; } = handlerType;
        public MethodInfo HandleMethod { get; } = handleMethod;
        public Type CommandType { get; } = commandType;
    }

    public CommandDispatcher(
        IServiceManager serviceManager,
        Lazy<IEventBus> eventBus)
    {
        ArgumentNullException.ThrowIfNull(serviceManager);
        ArgumentNullException.ThrowIfNull(eventBus);

        _serviceContainer = serviceManager.AnvilServiceContainer;
        _eventBus = eventBus;

        DiscoverAndCacheHandlers();
    }

    internal CommandDispatcher(
        IEnumerable<ICommandHandlerMarker> commandHandlers,
        IEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(commandHandlers);
        ArgumentNullException.ThrowIfNull(eventBus);

        _serviceContainer = null;
        _eventBus = new Lazy<IEventBus>(() => eventBus);

        DiscoverAndCacheHandlers(commandHandlers);
    }

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        Type commandType = typeof(TCommand);
        string commandTypeName = commandType.Name;

        Log.Debug("Dispatching command: {CommandType}", commandTypeName);

        try
        {
            if (!_handlerCache.TryGetValue(commandType, out HandlerInvocation? invocation))
            {
                string errorMessage =
                    $"No handler registered for command type: {commandTypeName}";

                Log.Error(errorMessage);
                return CommandResult.Fail(errorMessage);
            }

            object handler = ResolveHandler(invocation);

            Task<CommandResult> resultTask =
                (Task<CommandResult>)invocation.HandleMethod.Invoke(
                    handler,
                    new object[] { command, cancellationToken })!;

            CommandResult result =
                await resultTask.ConfigureAwait(false);

            if (result.Success)
            {
                await PublishCommandExecutedEventAsync(command, result)
                    .ConfigureAwait(false);
            }

            Log.Debug(
                "Command {CommandType} executed: {Success}",
                commandTypeName,
                result.Success
                    ? "Success"
                    : $"Failed - {result.ErrorMessage}");

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error dispatching command: {CommandType}",
                commandTypeName);

            return CommandResult.Fail(
                $"Command execution failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
        IEnumerable<TCommand> commands,
        BatchExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(commands);

        options ??= BatchExecutionOptions.Default;

        List<TCommand> commandsList = commands.ToList();
        List<CommandResult> results = new();

        Log.Debug(
            "Dispatching batch of {Count} commands with options: StopOnFirstFailure={StopOnFirst}",
            commandsList.Count,
            options.StopOnFirstFailure);

        foreach (TCommand command in commandsList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Log.Warn(
                    "Batch execution cancelled after {Executed} of {Total} commands",
                    results.Count,
                    commandsList.Count);

                return BatchCommandResult.FromResults(
                    results,
                    cancelled: true);
            }

            CommandResult result =
                await DispatchAsync(command, cancellationToken)
                    .ConfigureAwait(false);

            results.Add(result);

            if (!result.Success && options.StopOnFirstFailure)
            {
                Log.Warn(
                    "Batch execution stopped due to failure at command {Index} of {Total}",
                    results.Count,
                    commandsList.Count);

                break;
            }
        }

        BatchCommandResult batchResult =
            BatchCommandResult.FromResults(results);

        Log.Debug(
            "Batch execution completed: {Success}/{Total} succeeded",
            batchResult.SuccessCount,
            batchResult.TotalCount);

        return batchResult;
    }

    private void DiscoverAndCacheHandlers()
    {
        if (_serviceContainer == null)
        {
            throw new InvalidOperationException(
                "Cannot discover DI handler registrations without an Anvil service container.");
        }

        IEnumerable<IGrouping<Type, ServiceRegistration>> handlerRegistrationGroups =
            _serviceContainer.AvailableServices
                .Where(registration =>
                    registration.ImplementingType != null &&
                    typeof(ICommandHandlerMarker).IsAssignableFrom(
                        registration.ImplementingType))
                .GroupBy(registration => registration.ImplementingType!)
                .OrderBy(
                    group => group.Key.FullName,
                    StringComparer.Ordinal);

        foreach (IGrouping<Type, ServiceRegistration> registrationGroup
                 in handlerRegistrationGroups)
        {
            Type handlerType = registrationGroup.Key;

            CacheHandlerMetadata(
                handlerType,
                registrationGroup.ToArray(),
                handler: null);
        }

        Log.Info(
            "Command dispatcher initialized with {Count} command handler registration(s)",
            _handlerCache.Count);
    }

    private void DiscoverAndCacheHandlers(
        IEnumerable<ICommandHandlerMarker> commandHandlers)
    {
        foreach (ICommandHandlerMarker handler in commandHandlers)
        {
            CacheHandlerMetadata(
                handler.GetType(),
                registrations: null,
                handler);
        }

        Log.Debug(
            "Command dispatcher initialized with {Count} directly supplied test handler(s)",
            _handlerCache.Count);
    }

    private void CacheHandlerMetadata(
        Type handlerType,
        IReadOnlyCollection<ServiceRegistration>? registrations,
        object? handler)
    {
        IEnumerable<Type> handlerInterfaces =
            handlerType.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() ==
                    typeof(ICommandHandler<>));

        foreach (Type handlerInterface in handlerInterfaces)
        {
            Type commandType =
                handlerInterface.GetGenericArguments()[0];

            MethodInfo? handleMethod =
                handlerInterface.GetMethod(
                    nameof(ICommandHandler<ICommand>.HandleAsync));

            if (handleMethod == null)
            {
                Log.Warn(
                    "Could not find HandleAsync method on {HandlerType} for {CommandType}",
                    handlerType.Name,
                    commandType.Name);

                continue;
            }

            Type? serviceType = null;
            string? serviceName = null;

            if (handler == null)
            {
                ServiceRegistration? registration =
                    SelectBestRegistration(
                        registrations,
                        handlerInterface,
                        handlerType);

                if (registration == null)
                {
                    Log.Warn(
                        "Command handler {HandlerType} implements {HandlerInterface} " +
                        "but has no usable DI registration",
                        handlerType.Name,
                        handlerInterface.Name);

                    continue;
                }

                serviceType = registration.ServiceType;
                serviceName = registration.ServiceName;
            }

            HandlerInvocation invocation = new(
                serviceType,
                serviceName,
                handler,
                handlerType,
                handleMethod,
                commandType);

            if (_handlerCache.TryGetValue(
                    commandType,
                    out HandlerInvocation? existing))
            {
                Log.Warn(
                    "Multiple command handlers registered for {CommandType}: " +
                    "{ExistingHandler} and {NewHandler}. " +
                    "Using {NewHandler}.",
                    commandType.Name,
                    existing.HandlerType.Name,
                    handlerType.Name,
                    handlerType.Name);
            }

            _handlerCache[commandType] = invocation;
        }
    }

    private static ServiceRegistration? SelectBestRegistration(
        IReadOnlyCollection<ServiceRegistration>? registrations,
        Type handlerInterface,
        Type handlerType)
    {
        if (registrations == null || registrations.Count == 0)
        {
            return null;
        }

        return registrations
            .OrderBy(registration =>
                GetRegistrationPriority(
                    registration,
                    handlerInterface,
                    handlerType))
            .ThenBy(
                registration => registration.ServiceName,
                StringComparer.Ordinal)
            .First();
    }

    private static int GetRegistrationPriority(
        ServiceRegistration registration,
        Type handlerInterface,
        Type handlerType)
    {
        // Prefer the exact strongly typed command-handler registration.
        if (registration.ServiceType == handlerInterface)
        {
            return 0;
        }

        // Then prefer the explicit marker registration used by older handlers.
        if (registration.ServiceType == typeof(ICommandHandlerMarker))
        {
            return 1;
        }

        // A concrete self-registration is also sufficient to resolve the handler.
        if (registration.ServiceType == handlerType)
        {
            return 2;
        }

        // The implementing type is known to be an ICommandHandlerMarker, so any
        // remaining registration for that implementation can still resolve it.
        return 3;
    }

    private object ResolveHandler(HandlerInvocation invocation)
    {
        if (invocation.Handler != null)
        {
            return invocation.Handler;
        }

        if (_serviceContainer == null)
        {
            throw new InvalidOperationException(
                $"Handler {invocation.HandlerType.Name} has no service container available.");
        }

        if (invocation.ServiceType == null)
        {
            throw new InvalidOperationException(
                $"Handler {invocation.HandlerType.Name} has no resolvable service registration.");
        }

        if (string.IsNullOrWhiteSpace(invocation.ServiceName))
        {
            return _serviceContainer.GetInstance(
                invocation.ServiceType);
        }

        return _serviceContainer.GetInstance(
            invocation.ServiceType,
            invocation.ServiceName);
    }

    private async Task PublishCommandExecutedEventAsync<TCommand>(
        TCommand command,
        CommandResult result)
        where TCommand : ICommand
    {
        try
        {
            CommandExecutedEvent<TCommand> domainEvent =
                new(command, result);

            await _eventBus.Value
                .PublishAsync(domainEvent)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn(
                ex,
                "Failed to publish CommandExecutedEvent for command: {CommandType}",
                typeof(TCommand).Name);
        }
    }
}
