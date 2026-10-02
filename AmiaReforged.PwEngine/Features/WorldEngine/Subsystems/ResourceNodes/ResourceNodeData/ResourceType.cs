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

public static class ResourceTypeExtensions
{
    public static ResourceType ToResourceType(this string type) =>
        type.ToLowerInvariant() switch
        {
            "ore" => ResourceType.Ore,
            "geode" => ResourceType.Geode,
            "boulder" => ResourceType.Boulder,
            "tree" => ResourceType.Tree,
            "flora" => ResourceType.Flora,
            _ => ResourceType.Undefined
        };
}
