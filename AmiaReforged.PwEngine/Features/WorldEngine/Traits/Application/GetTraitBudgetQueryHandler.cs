using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Traits.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Application;

[ServiceBinding(typeof(IQueryHandler<GetTraitBudgetQuery, TraitBudget>))]
public class GetTraitBudgetQueryHandler(
    ICharacterTraitRepository characterTraitRepository,
    ITraitRepository traitRepository)
    : IQueryHandler<GetTraitBudgetQuery, TraitBudget>
{
    public Task<TraitBudget> HandleAsync(GetTraitBudgetQuery query, CancellationToken cancellationToken = default)
    {
        List<CharacterTrait> currentSelections = characterTraitRepository.GetByCharacterId(query.CharacterId);
        TraitBudget budget = CalculateBudget(currentSelections);
        return Task.FromResult(budget);
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

