namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Handler for executing commands.
/// Extends ICommandHandlerMarker for automatic discovery via Anvil DI.
/// </summary>
/// <typeparam name="TCommand">The command type to handle</typeparam>
public interface ICommandHandler<in TCommand> : ICommandHandlerMarker where TCommand : ICommand
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the command execution</returns>
    Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

