using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class ResourceDefinition
{
    public required string Name { get; set; }
    public ItemType Type { get; set; }
    public required List<string> Tags { get; init; }
}