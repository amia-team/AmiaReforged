using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class ResourceDefinition
{
    public required string Name { get; set; }
    public ItemType Type { get; set; }
    public required List<string> Tags { get; init; }
}