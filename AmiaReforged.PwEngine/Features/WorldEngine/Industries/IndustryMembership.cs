using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

public class IndustryMembership
{
    public Guid Id { get; init; }
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
    public required ProficiencyLevel Level { get; set; }
    public required List<CharacterKnowledge> CharacterKnowledge { get; init; }
}
