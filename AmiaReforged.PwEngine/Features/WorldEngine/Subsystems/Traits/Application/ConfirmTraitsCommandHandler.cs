using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(ICommandHandler<ConfirmTraitsCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class ConfirmTraitsCommandHandler(
    ICharacterTraitRepository characterTraitRepository,
    ITraitRepository traitRepository,
    IEventBus eventBus) : ICommandHandler<ConfirmTraitsCommand>
{
    public async Task<CommandResult> HandleAsync(ConfirmTraitsCommand command, CancellationToken cancellationToken = default)
    {
        List<CharacterTrait> currentSelections = characterTraitRepository.GetByCharacterId(command.CharacterId);

        // Calculate budget
        TraitBudget budget = CalculateBudget(currentSelections);

        // Cannot confirm if budget is negative
        if (budget.AvailablePoints < 0)
        {
            return CommandResult.Fail($"Cannot confirm traits: budget is negative ({budget.AvailablePoints} points)");
        }

        // Confirm all unconfirmed traits
        List<CharacterTrait> unconfirmedTraits = currentSelections.Where(ct => !ct.IsConfirmed).ToList();

        foreach (CharacterTrait trait in unconfirmedTraits)
        {
            trait.IsConfirmed = true;
            characterTraitRepository.Update(trait);
        }

        // Publish event
        await eventBus.PublishAsync(new TraitsConfirmedEvent(
            command.CharacterId,
            currentSelections.Count,
            budget.AvailablePoints,
            DateTime.UtcNow), cancellationToken);

        return CommandResult.OkWith("confirmedCount", unconfirmedTraits.Count);
    }

    private TraitBudget CalculateBudget(List<CharacterTrait> currentSelections)
    {
        int spentPoints = 0;
        foreach (CharacterTrait selection in currentSelections)
        {
            Trait? trait = traitRepository.Get(selection.TraitTag.Value);
            if (trait != null && selection.IsActive)
            {
                spentPoints += trait.PointCost;
            }
        }

        return new TraitBudget
        {
            SpentPoints = spentPoints,
            EarnedPoints = 0
        };
    }
}

