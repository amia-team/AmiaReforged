using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Traits.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Application;

[ServiceBinding(typeof(IQueryHandler<GetAllTraitsQuery, List<Trait>>))]
public class GetAllTraitsQueryHandler(ITraitRepository traitRepository)
    : IQueryHandler<GetAllTraitsQuery, List<Trait>>
{
    public Task<List<Trait>> HandleAsync(GetAllTraitsQuery query, CancellationToken cancellationToken = default)
    {
        List<Trait> traits = traitRepository.All();
        return Task.FromResult(traits);
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCharacterTraitsQuery, List<CharacterTrait>>))]
public class GetCharacterTraitsQueryHandler(ICharacterTraitRepository characterTraitRepository)
    : IQueryHandler<GetCharacterTraitsQuery, List<CharacterTrait>>
{
    public Task<List<CharacterTrait>> HandleAsync(GetCharacterTraitsQuery query, CancellationToken cancellationToken = default)
    {
        List<CharacterTrait> traits = characterTraitRepository.GetByCharacterId(query.CharacterId);
        return Task.FromResult(traits);
    }
}

