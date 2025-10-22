namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Domain service for validating trait selection and deselection rules.
/// </summary>
public static class TraitSelectionValidator
{
    /// <summary>
    /// Validates if a trait can be selected (with unlock tracking)
    /// </summary>
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
    /// Validates if a trait can be selected (without unlock tracking)
    /// </summary>
    public static bool CanSelect(
        Trait trait,
        ICharacterInfo character,
        List<CharacterTrait> currentSelections,
        TraitBudget budget)
    {
        // Check budget
        if (!budget.CanAfford(trait.PointCost))
            return false;

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

    public static bool CanDeselect(CharacterTrait characterTrait)
    {
        return !characterTrait.IsConfirmed;
    }

    private static bool IsRaceEligible(Trait trait, string characterRace)
    {
        // If forbidden, reject
        if (trait.ForbiddenRaces.Contains(characterRace))
            return false;

        // If no restrictions, allow
        if (trait.AllowedRaces.Count == 0)
            return true;

        // Check if race is in allowed list
        return trait.AllowedRaces.Contains(characterRace);
    }

    private static bool IsClassEligible(Trait trait, List<string> characterClasses)
    {
        // If any class is forbidden, reject
        if (trait.ForbiddenClasses.Any(fc => characterClasses.Contains(fc)))
            return false;

        // If no restrictions, allow
        if (trait.AllowedClasses.Count == 0)
            return true;

        // Check if any character class is in allowed list
        return trait.AllowedClasses.Any(ac => characterClasses.Contains(ac));
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
