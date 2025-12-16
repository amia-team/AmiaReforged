using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public class Industry
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public required List<Knowledge> Knowledge { get; init; }
    public List<Recipe> Recipes { get; init; } = [];
}
