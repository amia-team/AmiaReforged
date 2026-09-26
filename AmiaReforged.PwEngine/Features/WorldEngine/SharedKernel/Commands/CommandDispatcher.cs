using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Central command dispatcher implementation.
/// Routes commands through the CQRS handler registry and publishes domain events.
/// </summary>
[ServiceBinding(typeof(ICommandDispatcher))]
public sealed class CommandDispatcher : ICommandDispatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly CqrsRegistry _registry;
    private readonly Lazy<IEventBus> _eventBus;

    public CommandDispatcher(
        CqrsRegistry registry,
        Lazy<IEventBus> eventBus)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(eventBus);

        _registry = registry;
        _eventBus = eventBus;
    }

    internal CommandDispatcher(
        IEnumerable<ICommandHandlerMarker> commandHandlers,
        IEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(commandHandlers);
        ArgumentNullException.ThrowIfNull(eventBus);

        CqrsRegistry registry = new();

        foreach (ICommandHandlerMarker handler in commandHandlers)
        {
            registry.Register(handler);
        }

        _registry = registry;
        _eventBus = new Lazy<IEventBus>(() => eventBus);
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
            if (!_registry.Commands.TryResolve<TCommand>(
                    out ICommandHandler<TCommand>? handler))
            {
                string errorMessage =
                    $"No handler registered for command type: {commandTypeName}";

                Log.Error(errorMessage);
                return CommandResult.Fail(errorMessage);
            }

            CommandResult result =
                await handler.HandleAsync(command, cancellationToken)
                    .ConfigureAwait(false);

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
