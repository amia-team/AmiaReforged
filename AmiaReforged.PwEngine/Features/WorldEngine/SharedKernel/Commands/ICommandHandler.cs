using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Handler for executing commands.
/// Extends ICommandHandlerMarker for CQRS registry discovery.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
public interface ICommandHandler<in TCommand> : ICommandHandlerMarker
    where TCommand : ICommand
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the command execution.</returns>
    Task<CommandResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly registers this handler instance with a CQRS registry.
    /// This is an escape hatch for manual composition and tests; production
    /// currently discovers DI registrations through CqrsRegistry.
    /// </summary>
    void Register(CqrsRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Commands.Register<TCommand>(this);
    }
}
