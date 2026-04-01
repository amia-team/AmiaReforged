using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Default implementation of <see cref="IProficiencyProgressionService"/>.
/// Awards proficiency XP, auto-levels within tier boundaries, and enforces hard gates at tier ceilings.
/// </summary>
[ServiceBinding(typeof(IProficiencyProgressionService))]
public class ProficiencyProgressionService : IProficiencyProgressionService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ProficiencyXpResult AwardProficiencyXp(IndustryMembership membership, int xp)
    {
        if (xp <= 0)
        {
            return new ProficiencyXpResult
            {
                Success = false,
                NewLevel = membership.ProficiencyXpLevel,
                XpRemaining = membership.ProficiencyXp,
                XpRequired = ProficiencyXpCurve.XpForLevel(membership.ProficiencyXpLevel),
                LevelsGained = 0,
                IsAtTierCeiling = false,
                Message = "No XP to award."
            };
        }

        if (!CanGainXp(membership))
        {
            return ProficiencyXpResult.Blocked(membership.ProficiencyXpLevel,
                $"Cannot gain proficiency XP. Rank up from {membership.Level} to continue.");
        }

        int startLevel = membership.ProficiencyXpLevel;
        membership.ProficiencyXp += xp;

        // Level up as long as we have enough XP and are allowed to continue
        while (CanGainXp(membership))
        {
            int costForNext = ProficiencyXpCurve.XpForLevel(membership.ProficiencyXpLevel);
            if (costForNext <= 0) break; // At max level

            if (membership.ProficiencyXp < costForNext) break;

            membership.ProficiencyXp -= costForNext;
            membership.ProficiencyXpLevel++;

            // Sync the enum-based Level with the new tier when crossing boundaries
            ProficiencyLevel newTier = ProficiencyXpCurve.TierForLevel(membership.ProficiencyXpLevel);
            if (newTier != membership.Level && newTier > membership.Level)
            {
                // Don't auto-promote the enum Level — that requires an explicit rank-up.
                // But if we've entered Grandmaster (level 125), that's automatic.
                if (membership.ProficiencyXpLevel == ProficiencyXpCurve.MaxLevel)
                {
                    membership.Level = ProficiencyLevel.Grandmaster;
                }
            }

            Log.Info(
                $"Character {membership.CharacterId} leveled up to proficiency {membership.ProficiencyXpLevel} in {membership.IndustryTag}");
        }

        int levelsGained = membership.ProficiencyXpLevel - startLevel;
        bool atCeiling = IsAtTierCeiling(membership);

        // If at tier ceiling, discard any leftover XP (hard gate)
        if (atCeiling)
        {
            membership.ProficiencyXp = 0;
        }

        return new ProficiencyXpResult
        {
            Success = true,
            NewLevel = membership.ProficiencyXpLevel,
            XpRemaining = membership.ProficiencyXp,
            XpRequired = ProficiencyXpCurve.XpForLevel(membership.ProficiencyXpLevel),
            LevelsGained = levelsGained,
            IsAtTierCeiling = atCeiling,
            Message = atCeiling
                ? $"Reached tier ceiling at level {membership.ProficiencyXpLevel}. Rank up to continue."
                : levelsGained > 0
                    ? $"Gained {levelsGained} level(s). Now proficiency level {membership.ProficiencyXpLevel}."
                    : $"Gained XP. {membership.ProficiencyXp}/{ProficiencyXpCurve.XpForLevel(membership.ProficiencyXpLevel)} to next level."
        };
    }

    public int GetXpForNextLevel(int currentLevel) => ProficiencyXpCurve.XpForLevel(currentLevel);

    public bool CanGainXp(IndustryMembership membership)
    {
        // At max level — can't gain more
        if (membership.ProficiencyXpLevel >= ProficiencyXpCurve.MaxLevel) return false;

        // Master → Grandmaster promotion is automatic at level 125, so Master has no hard gate
        if (membership.Level == ProficiencyLevel.Master) return true;

        // At tier ceiling and hasn't ranked up past it
        return !IsAtTierCeiling(membership);
    }

    public ProficiencyLevel GetTierForLevel(int level) => ProficiencyXpCurve.TierForLevel(level);

    /// <summary>
    /// Checks if the membership is at the ceiling of the current enum-based tier
    /// (meaning they must rank up before gaining more XP levels).
    /// </summary>
    private static bool IsAtTierCeiling(IndustryMembership membership)
    {
        // If level is at or past the ceiling of the current enum Level tier, they need to rank up
        int ceiling = ProficiencyXpCurve.CeilingForTier(membership.Level);
        return ceiling > 0 && membership.ProficiencyXpLevel >= ceiling;
    }
}
