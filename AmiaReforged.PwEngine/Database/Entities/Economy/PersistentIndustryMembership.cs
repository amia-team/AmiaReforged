using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

public class PersistentIndustryMembership
{
    [Key] public required Guid Id { get; init; }

    public required Guid CharacterId { get; init; }
    [ForeignKey("CharacterId")] public PersistedCharacter? Character { get; init; }

    public required string IndustryTag { get; init; }

    public required ProficiencyLevel Level { get; set; }

    public required List<PersistentCharacterKnowledge> Knowledge { get; set; }
}
