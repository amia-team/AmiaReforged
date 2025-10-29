using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Traits.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Application;

[ServiceBinding(typeof(ICommandHandler<DeselectTraitCommand>))]
public class DeselectTraitCommandHandler(
    ICharacterTraitRepository characterTraitRepository,
    IEventBus eventBus) : ICommandHandler<DeselectTraitCommand>
{
    public async Task<CommandResult> HandleAsync(DeselectTraitCommand command, CancellationToken cancellationToken = default)
    {
        // Find the selection
        List<CharacterTrait> currentSelections = characterTraitRepository.GetByCharacterId(command.CharacterId);
        CharacterTrait? selection = currentSelections.FirstOrDefault(ct => ct.TraitTag.Value == command.TraitTag.Value);

        if (selection == null)
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' is not selected");
        }

        // Cannot deselect confirmed traits - they are permanent
        if (selection.IsConfirmed)
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' is confirmed and cannot be deselected");
        }

        // Delete the selection
        characterTraitRepository.Delete(selection.Id);

        // Publish event
        await eventBus.PublishAsync(new TraitDeselectedEvent(
            command.CharacterId,
            command.TraitTag,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.Ok();
    }
}

