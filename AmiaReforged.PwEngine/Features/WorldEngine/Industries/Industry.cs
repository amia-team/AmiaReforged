using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

public class Industry
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public required List<Knowledge> Knowledge { get; init; }
}
