using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public class Industry
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public required List<Knowledge> Knowledge { get; init; }
}
