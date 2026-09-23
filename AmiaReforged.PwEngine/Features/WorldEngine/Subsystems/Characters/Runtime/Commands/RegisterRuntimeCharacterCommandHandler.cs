using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Registers a runtime character with the repository.
/// </summary>
/// <remarks>
/// Registration is idempotent: when a character with the same id is already
/// cached, the request succeeds without replacing the existing instance.
/// Events are not published here; <c>CommandDispatcher</c> publishes the
/// successful <see cref="CommandExecutedEvent{T}"/> for us.
/// </remarks>
[ServiceBinding(typeof(ICommandHandler<RegisterRuntimeCharacterCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class RegisterRuntimeCharacterCommandHandler : ICommandHandler<RegisterRuntimeCharacterCommand>
{
    private readonly ICharacterRepository _repository;

    public RegisterRuntimeCharacterCommandHandler(ICharacterRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(
        RegisterRuntimeCharacterCommand command,
        CancellationToken cancellationToken = default)
    {
        CharacterId id = command.Character.GetId();

        if (_repository.Exists(id))
        {
            return Task.FromResult(CommandResult.Ok());
        }

        _repository.Add(command.Character);
        return Task.FromResult(CommandResult.Ok());
    }
}
