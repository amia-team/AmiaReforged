using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public class IndustryMembership
{
    public Guid Id { get; init; }
    public required Guid CharacterId { get; init; }
    public required string IndustryTag { get; init; }
    public required ProficiencyLevel Level { get; set; }
    public required List<CharacterKnowledge> CharacterKnowledge { get; init; }
}
