namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

/// <summary>
/// Domain event published when a command is successfully executed.
/// Enables event-driven reactions to command execution.
/// </summary>
public sealed record CommandExecutedEvent<TCommand> : IDomainEvent
    where TCommand : Commands.ICommand
{
    public CommandExecutedEvent(TCommand command, Commands.CommandResult result)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        OccurredAt = DateTime.UtcNow;
        EventId = Guid.NewGuid();
    }

    /// <summary>
    /// The command that was executed.
    /// </summary>
    public TCommand Command { get; }

    /// <summary>
    /// The result of the command execution.
    /// </summary>
    public Commands.CommandResult Result { get; }

    /// <summary>
    /// When the command was executed (UTC).
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// The command type name for logging/diagnostics.
    /// </summary>
    public string CommandTypeName => typeof(TCommand).Name;
}

