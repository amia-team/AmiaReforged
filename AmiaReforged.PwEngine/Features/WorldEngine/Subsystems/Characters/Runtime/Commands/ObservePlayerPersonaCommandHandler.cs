using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Upserts a player persona in the persistent persona repository.
/// </summary>
/// <remarks>
/// Upsert is idempotent: first and repeated observations both persist the
/// current display name and observation time. Events are not published here;
/// <c>CommandDispatcher</c> publishes the successful
/// <see cref="CommandExecutedEvent{TCommand}"/> for us.
/// </remarks>
[ServiceBinding(typeof(ICommandHandler<ObservePlayerPersonaCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class ObservePlayerPersonaCommandHandler : ICommandHandler<ObservePlayerPersonaCommand>
{
    private readonly IPersistentPlayerPersonaRepository _playerPersonas;

    public ObservePlayerPersonaCommandHandler(IPersistentPlayerPersonaRepository playerPersonas)
    {
        _playerPersonas = playerPersonas;
    }

    public Task<CommandResult> HandleAsync(
        ObservePlayerPersonaCommand command,
        CancellationToken cancellationToken = default)
    {
        _playerPersonas.Upsert(command.CdKey, command.DisplayName, command.ObservedUtc);
        return Task.FromResult(CommandResult.Ok());
    }
}
