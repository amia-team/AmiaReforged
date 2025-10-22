using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Domain service for validating trait selection and deselection rules.
/// </summary>
public static class TraitSelectionValidator
{
    /// <summary>
    /// Validates if a trait can be selected by a character (with unlock tracking).
    /// </summary>
    /// <param name="trait">The trait to validate</param>
    /// <param name="character">Character information (race, classes)</param>
    /// <param name="currentSelections">Currently selected traits</param>
    /// <param name="budget">Current trait budget (not enforced during selection)</param>
    /// <param name="unlockedTraits">Dictionary of unlocked trait tags</param>
    /// <returns>True if trait can be selected, false otherwise</returns>
    public static bool CanSelect(
        Trait trait,
        ICharacterInfo character,
        List<CharacterTrait> currentSelections,
        TraitBudget budget,
        Dictionary<string, bool> unlockedTraits)
    {
        // Check if trait requires unlock
        if (!IsUnlocked(trait, unlockedTraits))
            return false;

        return CanSelect(trait, character, currentSelections, budget);
    }

    /// <summary>
    /// Validates if a trait can be selected by a character (without unlock tracking).
    /// Note: Budget is NOT enforced here - players can go into debt with unconfirmed traits.
    /// Budget validation happens at confirmation time via ConfirmTraits().
    /// </summary>
    /// <param name="trait">The trait to validate</param>
    /// <param name="character">Character information (race, classes)</param>
    /// <param name="currentSelections">Currently selected traits</param>
    /// <param name="budget">Current trait budget (provided but not enforced)</param>
    /// <returns>True if trait can be selected, false otherwise</returns>
    /// <remarks>
    /// Validates: duplicate selection, race/class eligibility, conflicts, and prerequisites.
    /// Does NOT validate budget - that happens at confirmation.
    /// </remarks>
    public static bool CanSelect(
        Trait trait,
        ICharacterInfo character,
        List<CharacterTrait> currentSelections,
        TraitBudget budget)
    {
        // Check if already selected
        if (currentSelections.Any(ct => ct.TraitTag == trait.Tag))
            return false;

        // Check race eligibility
        if (!IsRaceEligible(trait, character.Race))
            return false;

        // Check class eligibility
        if (!IsClassEligible(trait, character.Classes))
            return false;

        // Check for conflicting traits
        if (HasConflictingTrait(trait, currentSelections))
            return false;

        // Check prerequisites
        if (!HasPrerequisites(trait, currentSelections))
            return false;

        return true;
    }

    private static bool IsUnlocked(Trait trait, Dictionary<string, bool> unlockedTraits)
    {
        // If trait doesn't require unlock, it's always available
        if (!trait.RequiresUnlock)
            return true;

        // Check if unlocked in dictionary
        return unlockedTraits.TryGetValue(trait.Tag, out bool unlocked) && unlocked;
    }

    /// <summary>
    /// Validates if a character trait can be deselected.
    /// </summary>
    /// <param name="characterTrait">The character trait to check</param>
    /// <returns>Always true - traits can be changed at any time</returns>
    /// <remarks>
    /// Confirmation marks the end of initial selection but does not permanently lock traits.
    /// Players can modify their traits post-creation.
    /// </remarks>
    public static bool CanDeselect(CharacterTrait characterTrait)
    {
        // Traits can always be deselected - confirmation just means the initial selection is finalized,
        // but players can change their traits later
        return true;
    }

    private static bool IsRaceEligible(Trait trait, RaceData characterRace)
    {
        // If forbidden, reject
        if (trait.ForbiddenRaces.Contains(characterRace.Name))
            return false;

        // If no restrictions, allow
        if (trait.AllowedRaces.Count == 0)
            return true;

        // Check if race is in allowed list
        return trait.AllowedRaces.Contains(characterRace.Name);
    }

    private static bool IsClassEligible(Trait trait, IReadOnlyList<CharacterClassData> characterClasses)
    {
        List<string> classNames = characterClasses.Select(c => c.Name).ToList();

        // If any class is forbidden, reject
        if (trait.ForbiddenClasses.Any(fc => classNames.Contains(fc)))
            return false;

        // If no restrictions, allow
        if (trait.AllowedClasses.Count == 0)
            return true;

        // Check if any character class is in allowed list
        return trait.AllowedClasses.Any(ac => classNames.Contains(ac));
    }

    private static bool HasConflictingTrait(Trait trait, List<CharacterTrait> currentSelections)
    {
        return trait.ConflictingTraits.Any(ct => 
            currentSelections.Any(cs => cs.TraitTag == ct));
    }

    private static bool HasPrerequisites(Trait trait, List<CharacterTrait> currentSelections)
    {
        // If no prerequisites, pass
        if (trait.PrerequisiteTraits.Count == 0)
            return true;

        // All prerequisites must be met
        return trait.PrerequisiteTraits.All(pt => 
            currentSelections.Any(cs => cs.TraitTag == pt));
    }
}
