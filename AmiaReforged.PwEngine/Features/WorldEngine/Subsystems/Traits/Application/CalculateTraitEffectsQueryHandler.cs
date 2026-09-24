using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;

[ServiceBinding(typeof(IQueryHandler<CalculateTraitEffectsQuery, TraitEffectsSummary>))]
public class CalculateTraitEffectsQueryHandler(
    ICharacterTraitRepository characterTraitRepository,
    ITraitRepository traitRepository)
    : IQueryHandler<CalculateTraitEffectsQuery, TraitEffectsSummary>
{
    public Task<TraitEffectsSummary> HandleAsync(
        CalculateTraitEffectsQuery query,
        CancellationToken cancellationToken = default)
    {
        List<CharacterTrait> characterTraits =
            characterTraitRepository.GetByCharacterId(query.CharacterId);

        Dictionary<string, int> statModifiers = new();
        List<string> specialAbilities = [];
        List<string> restrictions = [];

        foreach (CharacterTrait characterTrait in characterTraits)
        {
            if (!characterTrait.IsConfirmed || !characterTrait.IsActive)
                continue;

            Trait? definition = traitRepository.Get(characterTrait.TraitTag.Value);
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
            query.CharacterId, statModifiers, specialAbilities, restrictions));
    }
}
