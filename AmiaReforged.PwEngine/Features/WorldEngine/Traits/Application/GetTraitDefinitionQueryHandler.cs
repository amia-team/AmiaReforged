using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Traits.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Application;

[ServiceBinding(typeof(IQueryHandler<GetTraitDefinitionQuery, Trait?>))]
public class GetTraitDefinitionQueryHandler(ITraitRepository traitRepository)
    : IQueryHandler<GetTraitDefinitionQuery, Trait?>
{
    public Task<Trait?> HandleAsync(GetTraitDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        Trait? trait = traitRepository.Get(query.TraitTag.Value);
        return Task.FromResult(trait);
    }
}

