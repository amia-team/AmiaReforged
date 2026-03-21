using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

public class PersistentIndustryMembership
{
    [Key] public required Guid Id { get; init; }

    public required Guid CharacterId { get; init; }
    [ForeignKey("CharacterId")] public PersistedCharacter? Character { get; init; }

    public required string IndustryTag { get; init; }

    public required ProficiencyLevel Level { get; set; }

    /// <summary>
    /// Accumulated proficiency XP toward the next proficiency level.
    /// </summary>
    public int ProficiencyXp { get; set; }

    /// <summary>
    /// Current numeric proficiency level (1–125). 0 = Layman.
    /// </summary>
    public int ProficiencyXpLevel { get; set; }

    public required List<PersistentCharacterKnowledge> Knowledge { get; set; }
}
