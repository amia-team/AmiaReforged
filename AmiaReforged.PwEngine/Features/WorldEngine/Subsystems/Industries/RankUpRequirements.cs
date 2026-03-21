namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Defines the knowledge point cost required to rank up between proficiency tiers.
/// A character must be at the tier ceiling in proficiency XP level AND have spent
/// the required number of knowledge points in the industry to rank up.
/// </summary>
public static class RankUpRequirements
{
    /// <summary>
    /// Returns the number of knowledge points the character must have learned in the industry
    /// to rank up from <paramref name="currentTier"/> to the next tier.
    /// Returns 0 if the tier cannot be ranked up from (Layman or Grandmaster).
    /// </summary>
    public static int KnowledgePointsRequired(ProficiencyLevel currentTier)
    {
        return currentTier switch
        {
            ProficiencyLevel.Novice => 5,
            ProficiencyLevel.Apprentice => 10,
            ProficiencyLevel.Journeyman => 15,
            ProficiencyLevel.Expert => 20,
            ProficiencyLevel.Master => 25,
            _ => 0
        };
    }

    /// <summary>
    /// Returns the proficiency XP level that must be reached before ranking up from <paramref name="currentTier"/>.
    /// This is the ceiling of the current tier.
    /// </summary>
    public static int RequiredLevelForRankUp(ProficiencyLevel currentTier)
    {
        return ProficiencyXpCurve.CeilingForTier(currentTier);
    }
}
