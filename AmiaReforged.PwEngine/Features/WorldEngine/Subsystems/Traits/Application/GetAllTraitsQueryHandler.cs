using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

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
public class GetCharacterTraitsQueryHandler(
    ICharacterTraitRepository characterTraitRepository,
    ITraitRepository traitRepository)
    : IQueryHandler<GetCharacterTraitsQuery, List<CharacterTrait>>
{
    public Task<List<CharacterTrait>> HandleAsync(GetCharacterTraitsQuery query, CancellationToken cancellationToken = default)
    {
        List<CharacterTrait> baseTraits = characterTraitRepository.GetByCharacterId(query.CharacterId);

        // Enrich each selection with its display name (derived from the definition).
        // Falls back to the tag value when the definition is missing.
        List<CharacterTrait> traits = baseTraits.Select(selection =>
        {
            Trait? definition = traitRepository.Get(selection.TraitTag.Value);
            return new CharacterTrait
            {
                Id = selection.Id,
                CharacterId = selection.CharacterId,
                TraitTag = selection.TraitTag,
                Name = definition?.Name ?? selection.TraitTag.Value,
                DateAcquired = selection.DateAcquired,
                IsConfirmed = selection.IsConfirmed,
                IsActive = selection.IsActive,
                IsUnlocked = selection.IsUnlocked,
                CustomData = selection.CustomData
            };
        }).ToList();

        return Task.FromResult(traits);
    }
}

