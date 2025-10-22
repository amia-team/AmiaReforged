using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Effects;

/// <summary>
/// Application service for applying and removing trait effects.
/// Handles safe reapplication by tagging effects for removal.
/// </summary>
[ServiceBinding(typeof(TraitEffectApplicationService))]
public class TraitEffectApplicationService
{
    private readonly ICharacterTraitRepository _characterTraitRepository;
    private readonly ITraitRepository _traitRepository;

    /// <summary>
    /// Tag prefix for all trait-applied effects to enable safe removal.
    /// </summary>
    public const string EffectTagPrefix = "TRAIT_";

    public TraitEffectApplicationService(
        ICharacterTraitRepository characterTraitRepository,
        ITraitRepository traitRepository)
    {
        _characterTraitRepository = characterTraitRepository;
        _traitRepository = traitRepository;
    }

    /// <summary>
    /// Gets all effects that should be applied for a character.
    /// Only includes effects from active, confirmed traits.
    /// </summary>
    /// <param name="characterId">ID of the character</param>
    /// <returns>List of effects to apply with their source trait tags</returns>
    public List<(string TraitTag, TraitEffect Effect)> GetActiveEffects(Guid characterId)
    {
        List<CharacterTrait> traits = _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));
        List<(string TraitTag, TraitEffect Effect)> effects = new();

        foreach (CharacterTrait characterTrait in traits)
        {
            // Only apply effects from confirmed, active traits
            if (!characterTrait.IsConfirmed || !characterTrait.IsActive)
                continue;

            Trait? traitDefinition = _traitRepository.Get(characterTrait.TraitTag);
            if (traitDefinition == null)
                continue;

            // Add all effects from this trait
            foreach (TraitEffect effect in traitDefinition.Effects)
            {
                effects.Add((characterTrait.TraitTag, effect));
            }
        }

        return effects;
    }

    /// <summary>
    /// Generates an effect tag for tracking (enables safe removal/reapplication).
    /// </summary>
    /// <param name="traitTag">The trait that provides this effect</param>
    /// <param name="effectIndex">Index of the effect in the trait's effect list</param>
    /// <returns>Unique tag for this effect instance</returns>
    public static string GenerateEffectTag(string traitTag, int effectIndex)
    {
        return $"{EffectTagPrefix}{traitTag}_{effectIndex}";
    }

    /// <summary>
    /// Checks if a character has any traits with active effects.
    /// </summary>
    public bool HasActiveEffects(Guid characterId)
    {
        return GetActiveEffects(characterId).Any();
    }

    /// <summary>
    /// Factory method for testing.
    /// </summary>
    public static TraitEffectApplicationService Create(
        ICharacterTraitRepository characterTraitRepository,
        ITraitRepository traitRepository)
    {
        return new TraitEffectApplicationService(characterTraitRepository, traitRepository);
    }
}
