using System.Collections.Concurrent;
using System.Reflection;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
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

    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<Type, HandlerInvocation> _handlerCache = new();

    // Cached handler invocation data
    private sealed class HandlerInvocation
    {
        public object Handler { get; }
        public MethodInfo HandleMethod { get; }
        public Type CommandType { get; }

        public HandlerInvocation(object handler, MethodInfo handleMethod, Type commandType)
        {
            Handler = handler;
            HandleMethod = handleMethod;
            CommandType = commandType;
        }
    }

    public CommandDispatcher(
        IEnumerable<ICommandHandlerMarker> commandHandlers,
        IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        DiscoverAndCacheHandlers(commandHandlers);
    }

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
            // Get cached handler invocation
            if (!_handlerCache.TryGetValue(commandType, out HandlerInvocation? invocation))
            {
                string errorMessage = $"No handler registered for command type: {commandTypeName}";
                Log.Error(errorMessage);
                return CommandResult.Fail(errorMessage);
            }

            // Invoke the cached handler method
            Task<CommandResult> resultTask = (Task<CommandResult>)invocation.HandleMethod.Invoke(
                invocation.Handler,
                new object[] { command, cancellationToken })!;

            CommandResult result = await resultTask.ConfigureAwait(false);

            // Publish domain event if successful
            if (result.Success)
            {
                await PublishCommandExecutedEventAsync(command, result).ConfigureAwait(false);
            }

            Log.Debug("Command {CommandType} executed: {Success}",
                commandTypeName,
                result.Success ? "Success" : $"Failed - {result.ErrorMessage}");

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error dispatching command: {CommandType}", commandTypeName);
            return CommandResult.Fail($"Command execution failed: {ex.Message}");
        }
    }

    public async Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
        IEnumerable<TCommand> commands,
        BatchExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(commands);

        options ??= BatchExecutionOptions.Default;
        List<TCommand> commandsList = commands.ToList();
        List<CommandResult> results = new List<CommandResult>();

        Log.Debug("Dispatching batch of {Count} commands with options: StopOnFirstFailure={StopOnFirst}",
            commandsList.Count,
            options.StopOnFirstFailure);

        foreach (TCommand command in commandsList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Log.Warn("Batch execution cancelled after {Executed} of {Total} commands",
                    results.Count,
                    commandsList.Count);

                return BatchCommandResult.FromResults(results, cancelled: true);
            }

            CommandResult result = await DispatchAsync(command, cancellationToken).ConfigureAwait(false);
            results.Add(result);

            if (!result.Success && options.StopOnFirstFailure)
            {
                Log.Warn("Batch execution stopped due to failure at command {Index} of {Total}",
                    results.Count,
                    commandsList.Count);
                break;
            }
        }

        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        Log.Debug("Batch execution completed: {Success}/{Total} succeeded",
            batchResult.SuccessCount,
            batchResult.TotalCount);

        return batchResult;
    }

    private void DiscoverAndCacheHandlers(IEnumerable<ICommandHandlerMarker> commandHandlers)
    {
        foreach (ICommandHandlerMarker handler in commandHandlers)
        {
            Type handlerType = handler.GetType();
            IEnumerable<Type> handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));

            foreach (Type handlerInterface in handlerInterfaces)
            {
                Type commandType = handlerInterface.GetGenericArguments()[0];

                // Get the specific HandleAsync method for this command type
                MethodInfo? handleMethod = handlerInterface.GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync));

                if (handleMethod == null)
                {
                    Log.Warn("Could not find HandleAsync method on {HandlerType} for {CommandType}",
                        handlerType.Name,
                        commandType.Name);
                    continue;
                }

                // Cache the handler instance and its method
                HandlerInvocation invocation = new HandlerInvocation(handler, handleMethod, commandType);
                _handlerCache[commandType] = invocation;

                Log.Info("Registered and cached command handler {HandlerType} for command {CommandType}",
                    handlerType.Name,
                    commandType.Name);
            }
        }

        Log.Info("Command dispatcher initialized with {Count} command handler(s)", _handlerCache.Count);
    }

    private async Task PublishCommandExecutedEventAsync<TCommand>(TCommand command, CommandResult result)
        where TCommand : ICommand
    {
        try
        {
            CommandExecutedEvent<TCommand> domainEvent = new CommandExecutedEvent<TCommand>(command, result);
            await _eventBus.PublishAsync(domainEvent).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Event publishing failures should not fail the command
            Log.Warn(ex, "Failed to publish CommandExecutedEvent for command: {CommandType}", typeof(TCommand).Name);
        }
    }
}

