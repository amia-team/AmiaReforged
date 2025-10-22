using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Domain service handling trait lifecycle events related to character death.
/// Processes death behaviors for all active traits.
/// </summary>
[ServiceBinding(typeof(TraitDeathHandler))]
public class TraitDeathHandler
{
    private readonly ICharacterTraitRepository _characterTraitRepository;
    private readonly ITraitRepository _traitRepository;

    public TraitDeathHandler(
        ICharacterTraitRepository characterTraitRepository,
        ITraitRepository traitRepository)
    {
        _characterTraitRepository = characterTraitRepository;
        _traitRepository = traitRepository;
    }

    /// <summary>
    /// Processes death for all character traits based on their death behaviors.
    /// </summary>
    /// <param name="characterId">ID of the character who died</param>
    /// <param name="killedByHero">True if killed by a character with Hero trait</param>
    /// <returns>True if character should permadeath, false otherwise</returns>
    public bool ProcessDeath(Guid characterId, bool killedByHero = false)
    {
        List<CharacterTrait> traits = _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));
        bool shouldPermadeath = false;

        foreach (CharacterTrait characterTrait in traits)
        {
            Trait? traitDefinition = _traitRepository.Get(characterTrait.TraitTag);
            if (traitDefinition == null) continue;

            switch (traitDefinition.DeathBehavior)
            {
                case TraitDeathBehavior.Persist:
                    // Do nothing - trait persists unchanged
                    break;

                case TraitDeathBehavior.ResetOnDeath:
                    // Deactivate and clear custom data (Hero trait behavior)
                    characterTrait.IsActive = false;
                    characterTrait.CustomData = null;
                    _characterTraitRepository.Update(characterTrait);
                    break;

                case TraitDeathBehavior.Permadeath:
                    // Villain trait - permadeath only if killed by Hero
                    if (killedByHero)
                    {
                        shouldPermadeath = true;
                    }
                    break;

                case TraitDeathBehavior.RemoveOnDeath:
                    // Remove trait entirely
                    _characterTraitRepository.Delete(characterTrait.Id);
                    break;
            }
        }

        return shouldPermadeath;
    }

    /// <summary>
    /// Reactivates traits that were deactivated on death (Hero trait rebuilding).
    /// </summary>
    /// <param name="characterId">ID of the character</param>
    public void ReactivateResettableTraits(Guid characterId)
    {
        List<CharacterTrait> traits = _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));

        foreach (CharacterTrait characterTrait in traits.Where(t => !t.IsActive))
        {
            Trait? traitDefinition = _traitRepository.Get(characterTrait.TraitTag);
            if (traitDefinition?.DeathBehavior == TraitDeathBehavior.ResetOnDeath)
            {
                characterTrait.IsActive = true;
                _characterTraitRepository.Update(characterTrait);
            }
        }
    }

    /// <summary>
    /// Factory method for testing.
    /// </summary>
    public static TraitDeathHandler Create(
        ICharacterTraitRepository characterTraitRepository,
        ITraitRepository traitRepository)
    {
        return new TraitDeathHandler(characterTraitRepository, traitRepository);
    }
}
