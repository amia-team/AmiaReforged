using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceType
{
    Undefined = 0,
    Ore = 1,
    Geode = 2,
    Boulder = 3,
    Tree = 4,
    Flora = 5
}
