using Anvil.Services;

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
    public bool SelectTrait(
        Guid characterId,
        string traitTag,
        ICharacterInfo character,
        Dictionary<string, bool> unlockedTraits)
    {
        Trait? trait = _traitRepository.Get(traitTag);
        if (trait == null)
            return false;

        List<CharacterTrait> currentSelections = _characterTraitRepository.GetByCharacterId(characterId);
        TraitBudget budget = CalculateBudget(currentSelections);

        if (!TraitSelectionValidator.CanSelect(trait, character, currentSelections, budget, unlockedTraits))
            return false;

        CharacterTrait newSelection = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            TraitTag = traitTag,
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
    public bool DeselectTrait(Guid characterId, string traitTag)
    {
        List<CharacterTrait> currentSelections = _characterTraitRepository.GetByCharacterId(characterId);
        CharacterTrait? selection = currentSelections.FirstOrDefault(ct => ct.TraitTag == traitTag);

        if (selection == null)
            return false;

        if (!TraitSelectionValidator.CanDeselect(selection))
            return false;

        _characterTraitRepository.Delete(selection.Id);
        return true;
    }

    /// <summary>
    /// Confirms all unconfirmed traits for a character, finalizing the initial selection.
    /// Note: Confirmed traits can still be changed later - this just marks the end of the initial selection phase.
    /// </summary>
    public bool ConfirmTraits(Guid characterId)
    {
        List<CharacterTrait> currentSelections = _characterTraitRepository.GetByCharacterId(characterId);
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
    public List<CharacterTrait> GetCharacterTraits(Guid characterId)
    {
        return _characterTraitRepository.GetByCharacterId(characterId);
    }

    /// <summary>
    /// Calculates the current budget for a character based on their selections.
    /// </summary>
    public TraitBudget CalculateBudget(List<CharacterTrait> currentSelections)
    {
        int spentPoints = 0;
        foreach (CharacterTrait selection in currentSelections)
        {
            Trait? trait = _traitRepository.Get(selection.TraitTag);
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
