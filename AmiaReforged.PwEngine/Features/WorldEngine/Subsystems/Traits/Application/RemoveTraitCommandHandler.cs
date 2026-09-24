using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(ICommandHandler<RemoveTraitCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class RemoveTraitCommandHandler(
    ICharacterTraitRepository characterTraitRepository) : ICommandHandler<RemoveTraitCommand>
{
    public Task<CommandResult> HandleAsync(RemoveTraitCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // The character must hold the trait; removing an absent trait fails.
            List<CharacterTrait> traits = characterTraitRepository.GetByCharacterId(command.CharacterId);
            CharacterTrait? match = traits.FirstOrDefault(t => t.TraitTag == command.TraitTag);
            if (match == null)
            {
                return Task.FromResult(CommandResult.Fail($"Character does not have trait '{command.TraitTag.Value}'."));
            }

            // Removal persists: the selection is deleted from the character's trait list.
            characterTraitRepository.Delete(match.Id);

            // The dispatcher publishes the generic CommandExecutedEvent on success.
            return Task.FromResult(CommandResult.Ok());
        }
        catch (Exception exception)
        {
            return Task.FromException<CommandResult>(exception);
        }
    }
}
