namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Service for managing proficiency XP progression within an industry membership.
/// Proficiency XP is a per-industry meter (1–125 levels) with a logarithmic cost curve.
/// </summary>
public interface IProficiencyProgressionService
{
    /// <summary>
    /// Awards proficiency XP to the given industry membership. Automatically levels up
    /// if enough XP is accumulated, respecting tier boundaries (hard gate).
    /// </summary>
    /// <param name="membership">The industry membership to award XP to. Will be mutated.</param>
    /// <param name="xp">Amount of proficiency XP to award.</param>
    /// <returns>Result describing what happened (levels gained, tier ceiling status, etc.).</returns>
    ProficiencyXpResult AwardProficiencyXp(IndustryMembership membership, int xp);

    /// <summary>
    /// Returns the XP required to advance from <paramref name="currentLevel"/> to the next level.
    /// </summary>
    int GetXpForNextLevel(int currentLevel);

    /// <summary>
    /// Returns true if the membership can currently gain proficiency XP.
    /// False if at tier ceiling without having ranked up, or at max level.
    /// </summary>
    bool CanGainXp(IndustryMembership membership);

    /// <summary>
    /// Returns the <see cref="ProficiencyLevel"/> tier for the given numeric level.
    /// </summary>
    ProficiencyLevel GetTierForLevel(int level);
}
