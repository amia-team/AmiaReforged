using Anvil.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Application service orchestrating trait selection, deselection, and confirmation.
/// </summary>
[ServiceBinding(typeof(TraitSelectionService))]
public class TraitSelectionService
{
    private readonly ICharacterTraitRepository _characterTraitRepository;
    private readonly ITraitRepository _traitRepository;

    public TraitSelectionService(
        ICharacterTraitRepository characterTraitRepository,
        ITraitRepository traitRepository)
    {
        _characterTraitRepository = characterTraitRepository;
        _traitRepository = traitRepository;
    }

    /// <summary>
    /// Attempts to select a trait for a character.
    /// </summary>
    /// <param name="characterId">ID of the character selecting the trait</param>
    /// <param name="traitTag">Tag of the trait to select</param>
    /// <param name="character">Character information for eligibility validation</param>
    /// <param name="unlockedTraits">Dictionary of unlocked trait tags</param>
    /// <returns>True if selection succeeded, false if trait doesn't exist or validation failed</returns>
    /// <remarks>
    /// Creates an unconfirmed trait selection. Players can go into debt during selection.
    /// Budget is validated at confirmation time.
    /// </remarks>
    public bool SelectTrait(
        Guid characterId,
        string traitTag,
        ICharacterInfo character,
        Dictionary<string, bool> unlockedTraits)
    {
        Trait? trait = _traitRepository.Get(traitTag);
        if (trait == null)
            return false;

        List<CharacterTrait> currentSelections = _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));
        TraitBudget budget = CalculateBudget(currentSelections);

        if (!TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget, unlockedTraits))
            return false;

        CharacterTrait newSelection = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag(traitTag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = false,
            IsActive = true,
            IsUnlocked = trait.RequiresUnlock
        };

        _characterTraitRepository.Add(newSelection);
        return true;
    }

    /// <summary>
    /// Attempts to deselect a trait for a character.
    /// </summary>
    /// <param name="characterId">ID of the character</param>
    /// <param name="traitTag">Tag of the trait to deselect</param>
    /// <returns>True if deselection succeeded, false if trait not found or cannot be deselected</returns>
    public bool DeselectTrait(Guid characterId, string traitTag)
    {
        List<CharacterTrait> currentSelections = _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));
        CharacterTrait? selection = currentSelections.FirstOrDefault(ct => ct.TraitTag.Value == traitTag);

        if (selection == null)
            return false;

        if (!TraitSelectionValidator.CanDeselect(selection))
            return false;

        _characterTraitRepository.Delete(selection.Id);
        return true;
    }

    /// <summary>
    /// Confirms all unconfirmed traits for a character, finalizing the initial selection.
    /// </summary>
    /// <param name="characterId">ID of the character</param>
    /// <returns>True if confirmation succeeded, false if budget is negative</returns>
    /// <remarks>
    /// Validates that the character's budget is non-negative before confirming.
    /// Confirmation marks the end of initial trait selection but does not permanently lock traits.
    /// Players can still modify traits after confirmation.
    /// </remarks>
    public bool ConfirmTraits(Guid characterId)
    {
        List<CharacterTrait> currentSelections = _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));
        TraitBudget budget = CalculateBudget(currentSelections);

        // Cannot confirm if budget is negative
        if (budget.AvailablePoints < 0)
            return false;

        List<CharacterTrait> unconfirmedTraits = currentSelections.Where(ct => !ct.IsConfirmed).ToList();

        foreach (CharacterTrait trait in unconfirmedTraits)
        {
            trait.IsConfirmed = true;
            _characterTraitRepository.Update(trait);
        }

        return true;
    }

    /// <summary>
    /// Gets all traits selected by a character.
    /// </summary>
    /// <param name="characterId">ID of the character</param>
    /// <returns>List of all character traits (confirmed and unconfirmed)</returns>
    public List<CharacterTrait> GetCharacterTraits(Guid characterId)
    {
        return _characterTraitRepository.GetByCharacterId(CharacterId.From(characterId));
    }

    /// <summary>
    /// Calculates the current budget for a character based on their trait selections.
    /// </summary>
    /// <param name="currentSelections">List of character's current trait selections</param>
    /// <returns>Budget showing total, spent, and available points</returns>
    /// <remarks>
    /// Only counts points from active traits. Negative cost traits (drawbacks) reduce spent points.
    /// Currently does not track earned points - that will be added in Phase 4.
    /// </remarks>
    public TraitBudget CalculateBudget(List<CharacterTrait> currentSelections)
    {
        int spentPoints = 0;
        foreach (CharacterTrait selection in currentSelections)
        {
            Trait? trait = _traitRepository.Get(selection.TraitTag.Value);
            if (trait != null && selection.IsActive)
            {
                spentPoints += trait.PointCost;
            }
        }

        return new TraitBudget
        {
            SpentPoints = spentPoints,
            EarnedPoints = 0 // Will be extended later for earned points
        };
    }

    public static TraitSelectionService Create(
        ICharacterTraitRepository characterTraitRepository,
        ITraitRepository traitRepository)
    {
        return new TraitSelectionService(characterTraitRepository, traitRepository);
    }
}
