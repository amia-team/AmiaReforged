using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Removes a runtime character from the repository.
/// </summary>
/// <remarks>
/// Removal is idempotent: when no character with the given id is cached, the
/// request still succeeds. Events are not published here; <c>CommandDispatcher</c>
/// publishes the successful <see cref="CommandExecutedEvent{T}"/> for us.
/// </remarks>
[ServiceBinding(typeof(ICommandHandler<RemoveRuntimeCharacterCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class RemoveRuntimeCharacterCommandHandler : ICommandHandler<RemoveRuntimeCharacterCommand>
{
    private readonly ICharacterRepository _repository;

    public RemoveRuntimeCharacterCommandHandler(ICharacterRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(
        RemoveRuntimeCharacterCommand command,
        CancellationToken cancellationToken = default)
    {
        _repository.DeleteById(command.CharacterId.Value);
        return Task.FromResult(CommandResult.Ok());
    }
}
