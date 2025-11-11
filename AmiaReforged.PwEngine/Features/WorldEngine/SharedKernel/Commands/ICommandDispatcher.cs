namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Central dispatcher for routing commands to their registered handlers.
/// Resolves handlers from DI and provides execution infrastructure.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Executes a command by resolving and invoking its handler.
    /// </summary>
    /// <typeparam name="TCommand">The command type implementing ICommand.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Executes multiple commands according to the specified options.
    /// </summary>
    /// <typeparam name="TCommand">The command type implementing ICommand.</typeparam>
    /// <param name="commands">The commands to execute.</param>
    /// <param name="options">Execution options. If null, uses defaults.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate result of all command executions.</returns>
    Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
        IEnumerable<TCommand> commands,
        BatchExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}

