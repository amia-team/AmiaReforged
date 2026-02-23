using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;

public class RegionDefinition
{
    public required RegionTag Tag { get; set; }
    public required string Name { get; set; }
    public List<AreaDefinition> Areas { get; set; } = [];

    /// <summary>
    /// Default chaos state for this region. Areas inherit this unless they have an override.
    /// </summary>
    public ChaosState? DefaultChaos { get; set; }
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
