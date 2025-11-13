using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(ICommandHandler<SetTraitActiveCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class SetTraitActiveCommandHandler(
    ICharacterTraitRepository characterTraitRepository,
    IEventBus eventBus) : ICommandHandler<SetTraitActiveCommand>
{
    public async Task<CommandResult> HandleAsync(SetTraitActiveCommand command, CancellationToken cancellationToken = default)
    {
        // Find the selection
        List<CharacterTrait> currentSelections = characterTraitRepository.GetByCharacterId(command.CharacterId);
        CharacterTrait? selection = currentSelections.FirstOrDefault(ct => ct.TraitTag.Value == command.TraitTag.Value);

        if (selection == null)
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' is not selected");
        }

        // Update active state
        if (selection.IsActive == command.IsActive)
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' is already {(command.IsActive ? "active" : "inactive")}");
        }

        selection.IsActive = command.IsActive;
        characterTraitRepository.Update(selection);

        // Publish event
        await eventBus.PublishAsync(new TraitActiveStateChangedEvent(
            command.CharacterId,
            command.TraitTag,
            command.IsActive,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.Ok();
    }
}


[ServiceBinding(typeof(ICommandHandler<SelectTraitCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class SelectTraitCommandHandler(
    ICharacterTraitRepository characterTraitRepository,
    ITraitRepository traitRepository,
    IEventBus eventBus) : ICommandHandler<SelectTraitCommand>
{
    public async Task<CommandResult> HandleAsync(SelectTraitCommand command, CancellationToken cancellationToken = default)
    {
        // Validate trait exists
        Trait? trait = traitRepository.Get(command.TraitTag.Value);
        if (trait == null)
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' not found");
        }

        // Check if already selected
        List<CharacterTrait> currentSelections = characterTraitRepository.GetByCharacterId(command.CharacterId);
        if (currentSelections.Any(ct => ct.TraitTag.Value == command.TraitTag.Value))
        {
            return CommandResult.Fail($"Trait '{command.TraitTag.Value}' is already selected");
        }

        // Check if trait requires unlock
        if (trait.RequiresUnlock)
        {
            if (!command.UnlockedTraits.TryGetValue(command.TraitTag.Value, out bool isUnlocked) || !isUnlocked)
            {
                return CommandResult.Fail($"Trait '{command.TraitTag.Value}' requires unlock");
            }
        }

        // Create new selection
        CharacterTrait newSelection = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = command.CharacterId,
            TraitTag = command.TraitTag,
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = false,
            IsActive = true,
            IsUnlocked = trait.RequiresUnlock
        };

        // Save to repository
        characterTraitRepository.Add(newSelection);

        // Publish event
        await eventBus.PublishAsync(new TraitSelectedEvent(
            command.CharacterId,
            command.TraitTag,
            false,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("characterTraitId", newSelection.Id);
    }
}

