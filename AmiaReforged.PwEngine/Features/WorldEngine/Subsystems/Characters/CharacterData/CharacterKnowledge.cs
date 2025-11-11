using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

public class CharacterKnowledge
{
    public Guid Id { get; init; }
    public required string IndustryTag { get; init; }
    public required Knowledge Definition { get; init; }
    public Guid CharacterId { get; set; }
}
