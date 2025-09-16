using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public class CharacterKnowledge
{
    public Guid Id { get; init; }
    public required string IndustryTag { get; init; }
    public required Knowledge Definition { get; init; }
    public Guid CharacterId { get; set; }
}
