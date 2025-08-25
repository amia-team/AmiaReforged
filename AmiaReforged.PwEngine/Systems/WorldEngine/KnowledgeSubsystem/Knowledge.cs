using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

public class Knowledge
{
    public required string Tag { get; init; }
    public required string Name { get; init; }
    public required ProficiencyLevel Level { get; init; }
    public int PointCost { get; set; }
}
