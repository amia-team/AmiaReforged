using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public class IndustryMembership
{
    public Guid Id { get; init; }
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
    public required ProficiencyLevel Level { get; set; }
    public required List<CharacterKnowledge> CharacterKnowledge { get; init; }

    /// <summary>
    /// Accumulated proficiency XP toward the next proficiency level (resets on level-up).
    /// </summary>
    public int ProficiencyXp { get; set; }

    /// <summary>
    /// Current numeric proficiency level (1–125). 0 means Layman (pre-membership).
    /// Tiers: Novice 1–25, Apprentice 26–50, Journeyman 51–75, Expert 76–100, Master 101–124, Grandmaster 125.
    /// </summary>
    public int ProficiencyXpLevel { get; set; }

    /// <summary>
    /// Returns the <see cref="ProficiencyLevel"/> tier derived from <see cref="ProficiencyXpLevel"/>.
    /// </summary>
    public ProficiencyLevel ProficiencyTier => ProficiencyXpCurve.TierForLevel(ProficiencyXpLevel);
}
