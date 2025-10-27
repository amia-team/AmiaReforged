using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

public class RegionDefinition
{
    public required string Tag { get; set; }
    public required string Name { get; set; }
    public List<AreaDefinition> Areas { get; set; } = [];
    // Settlement IDs associated with this region (predefined constants across Amia)
    public List<int> Settlements { get; set; } = [];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Climate
{
    Undefined,
    Temperate,
    Tropical,
    Arid,
    Arctic,
    Mediterranean,
    Continental,
    Oceanic,
    Subarctic,
    Alpine,
    Tundra,
    Desert
}
