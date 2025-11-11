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
}
