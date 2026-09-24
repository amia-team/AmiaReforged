using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(IQueryHandler<HasTraitAsyncQuery, bool>))]
public class HasTraitAsyncQueryHandler(
    ICharacterTraitRepository characterTraitRepository,
    ITraitRepository traitRepository)
    : IQueryHandler<HasTraitAsyncQuery, bool>
{
    public Task<bool> HandleAsync(HasTraitAsyncQuery query, CancellationToken cancellationToken = default)
    {
        // A character cannot "have" a trait that has no definition.
        if (traitRepository.Get(query.TraitTag.Value) is not Trait)
            return Task.FromResult(false);

        CharacterTrait? match = characterTraitRepository
            .GetByCharacterId(query.CharacterId)
            .FirstOrDefault(t => t.TraitTag == query.TraitTag && t.IsActive);

        return Task.FromResult(match is not null);
    }
}
