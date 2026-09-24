using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Marks a player persona active in the persistent persona repository.
/// </summary>
/// <remarks>
/// Touch is idempotent: repeated calls simply refresh the activity timestamp.
/// Events are not published here; <c>CommandDispatcher</c> publishes the
/// successful <see cref="CommandExecutedEvent{TCommand}"/> for us.
/// </remarks>
[ServiceBinding(typeof(ICommandHandler<TouchPlayerPersonaCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class TouchPlayerPersonaCommandHandler : ICommandHandler<TouchPlayerPersonaCommand>
{
    private readonly IPersistentPlayerPersonaRepository _playerPersonas;

    public TouchPlayerPersonaCommandHandler(IPersistentPlayerPersonaRepository playerPersonas)
    {
        _playerPersonas = playerPersonas;
    }

    public Task<CommandResult> HandleAsync(
        TouchPlayerPersonaCommand command,
        CancellationToken cancellationToken = default)
    {
        _playerPersonas.Touch(command.CdKey, command.ActivatedUtc);
        return Task.FromResult(CommandResult.Ok());
    }
}
