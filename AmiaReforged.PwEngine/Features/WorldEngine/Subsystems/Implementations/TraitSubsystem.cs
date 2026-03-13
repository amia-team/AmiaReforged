using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Anvil.Services;
using DomainCharacterTrait = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.CharacterTrait;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Production implementation of the Trait subsystem.
/// Delegates to ITraitRepository (in-memory trait definitions) and ICharacterTraitRepository (DB-persisted character traits).
/// </summary>
[ServiceBinding(typeof(ITraitSubsystem))]
public sealed class TraitSubsystem : ITraitSubsystem
{
    private readonly ITraitRepository _traitRepository;
    private readonly ICharacterTraitRepository _characterTraitRepository;

    public TraitSubsystem(
        ITraitRepository traitRepository,
        ICharacterTraitRepository characterTraitRepository)
    {
        _traitRepository = traitRepository;
        _characterTraitRepository = characterTraitRepository;
    }

    public Task<TraitDefinition?> GetTraitAsync(TraitTag traitTag, CancellationToken ct = default)
    {
        Trait? trait = _traitRepository.Get(traitTag);
        return Task.FromResult(trait != null ? MapToDefinition(trait) : null);
    }

    public Task<List<TraitDefinition>> GetAllTraitsAsync(CancellationToken ct = default)
    {
        List<TraitDefinition> definitions = _traitRepository.All()
            .Select(MapToDefinition)
            .ToList();
        return Task.FromResult(definitions);
    }

    public Task<CommandResult> GrantTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        Trait? trait = _traitRepository.Get(traitTag);
        if (trait == null)
            return Task.FromResult(CommandResult.Fail($"Trait '{traitTag.Value}' does not exist."));

        // Check if character already has this trait
        List<DomainCharacterTrait> existing = _characterTraitRepository.GetByCharacterId(characterId);
        if (existing.Any(ct2 => ct2.TraitTag == traitTag))
            return Task.FromResult(CommandResult.Fail($"Character already has trait '{trait.Name}'."));

        DomainCharacterTrait characterTrait = new DomainCharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            TraitTag = traitTag,
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true,
            IsUnlocked = trait.RequiresUnlock
        };

        _characterTraitRepository.Add(characterTrait);
        return Task.FromResult(CommandResult.Ok());
    }

    public Task<CommandResult> RemoveTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        List<DomainCharacterTrait> traits = _characterTraitRepository.GetByCharacterId(characterId);
        DomainCharacterTrait? match = traits.FirstOrDefault(t => t.TraitTag == traitTag);

        if (match == null)
            return Task.FromResult(CommandResult.Fail($"Character does not have trait '{traitTag.Value}'."));

        _characterTraitRepository.Delete(match.Id);
        return Task.FromResult(CommandResult.Ok());
    }

    public Task<List<CharacterTrait>> GetCharacterTraitsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        List<DomainCharacterTrait> domainTraits = _characterTraitRepository.GetByCharacterId(characterId);

        List<CharacterTrait> result = domainTraits.Select(dt =>
        {
            Trait? definition = _traitRepository.Get(dt.TraitTag);
            return new CharacterTrait(
                dt.TraitTag,
                definition?.Name ?? dt.TraitTag.Value,
                dt.DateAcquired,
                null);
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<bool> HasTraitAsync(CharacterId characterId, TraitTag traitTag, CancellationToken ct = default)
    {
        List<DomainCharacterTrait> traits = _characterTraitRepository.GetByCharacterId(characterId);
        bool has = traits.Any(t => t.TraitTag == traitTag && t.IsActive);
        return Task.FromResult(has);
    }

    public Task<TraitEffectsSummary> CalculateTraitEffectsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        List<DomainCharacterTrait> characterTraits = _characterTraitRepository.GetByCharacterId(characterId);

        Dictionary<string, int> statModifiers = new();
        List<string> specialAbilities = [];
        List<string> restrictions = [];

        foreach (DomainCharacterTrait characterTrait in characterTraits)
        {
            if (!characterTrait.IsConfirmed || !characterTrait.IsActive)
                continue;

            Trait? definition = _traitRepository.Get(characterTrait.TraitTag);
            if (definition == null)
                continue;

            foreach (TraitEffect effect in definition.Effects)
            {
                switch (effect.EffectType)
                {
                    case TraitEffectType.SkillModifier:
                    case TraitEffectType.AttributeModifier:
                    case TraitEffectType.KnowledgePoints:
                        string key = $"{effect.EffectType}:{effect.Target}";
                        statModifiers[key] = statModifiers.GetValueOrDefault(key) + effect.Magnitude;
                        break;

                    case TraitEffectType.Custom:
                        if (!string.IsNullOrWhiteSpace(effect.Description))
                            specialAbilities.Add(effect.Description);
                        break;
                }
            }
        }

        return Task.FromResult(new TraitEffectsSummary(
            characterId, statModifiers, specialAbilities, restrictions));
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

