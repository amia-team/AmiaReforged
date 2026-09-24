using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;
using Anvil.Services;
using DomainCharacterTrait = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.CharacterTrait;
using ITraitRepository = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.ITraitRepository;
using ICharacterTraitRepository = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.ICharacterTraitRepository;
using Trait = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Trait;

// The public projection CharacterTrait (in ...Subsystems) is distinct from the
// entity CharacterTrait (in ...Traits); alias the projection to remove ambiguity.
using PublicCharacterTrait = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.CharacterTrait;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Production implementation of the Trait subsystem.
/// Delegates to ITraitRepository (in-memory trait definitions) and ICharacterTraitRepository (DB-persisted character traits).
/// </summary>
[ServiceBinding(typeof(ITraitSubsystem))]
public sealed class TraitSubsystem : ITraitSubsystem
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ITraitRepository _traitRepository;
    private readonly ICharacterTraitRepository _characterTraitRepository;

    public TraitSubsystem(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ITraitRepository traitRepository,
        ICharacterTraitRepository characterTraitRepository)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _traitRepository = traitRepository;
        _characterTraitRepository = characterTraitRepository;
    }

    public async Task<TraitDefinition?> GetTraitAsync(TraitTag traitTag, CancellationToken ct = default)
    {
        Trait? trait = await _queryDispatcher.DispatchAsync<GetTraitDefinitionQuery, Trait?>(new GetTraitDefinitionQuery(traitTag), ct);
        return trait is null ? null : MapToDefinition(trait);
    }

    public async Task<List<TraitDefinition>> GetAllTraitsAsync(CancellationToken ct = default)
    {
        List<Trait> traits = await _queryDispatcher.DispatchAsync<GetAllTraitsQuery, List<Trait>>(new GetAllTraitsQuery(), ct);
        return traits.Select(MapToDefinition).ToList();
    }

    public Task<CommandResult> GrantTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        GrantTraitCommand command = new(characterId, traitTag);
        return _commandDispatcher.DispatchAsync(command, ct);
    }

    public Task<CommandResult> RemoveTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        RemoveTraitCommand command = new(characterId, traitTag);
        return _commandDispatcher.DispatchAsync(command, ct);
    }

    public async Task<List<PublicCharacterTrait>> GetCharacterTraitsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        List<DomainCharacterTrait> domainTraits = await _queryDispatcher.DispatchAsync<GetCharacterTraitsQuery, List<DomainCharacterTrait>>(new GetCharacterTraitsQuery(characterId), ct);

        List<PublicCharacterTrait> result = domainTraits.Select(dt => new PublicCharacterTrait(
            dt.TraitTag,
            dt.Name!,
            dt.DateAcquired,
            null)).ToList();

        return result;
    }

    public async Task<bool> HasTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        bool has = await _queryDispatcher.DispatchAsync<HasTraitAsyncQuery, bool>(new HasTraitAsyncQuery(characterId, traitTag), ct);
        return has;
    }

    public async Task<TraitEffectsSummary> CalculateTraitEffectsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        TraitEffectsSummary effects = await _queryDispatcher.DispatchAsync<CalculateTraitEffectsQuery, TraitEffectsSummary>(
            new CalculateTraitEffectsQuery(characterId), ct);
        return effects;
    }

    private static TraitDefinition MapToDefinition(Trait trait)
    {
        Dictionary<string, object> effects = new();
        for (int i = 0; i < trait.Effects.Count; i++)
        {
            TraitEffect e = trait.Effects[i];
            effects[$"{e.EffectType}:{e.Target ?? "general"}"] = new
            {
                e.EffectType,
                e.Target,
                e.Magnitude,
                e.Description
            };
        }

        return new TraitDefinition(
            new TraitTag(trait.Tag),
            trait.Name,
            trait.Description,
            trait.Category,
            trait.PointCost,
            trait.DeathBehavior,
            trait.RequiresUnlock,
            trait.DmOnly,
            effects);
    }
}

