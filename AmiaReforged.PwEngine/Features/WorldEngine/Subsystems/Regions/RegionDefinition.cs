using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;

public class RegionDefinition
{
    public required RegionTag Tag { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// Free-form, human-readable description of the region.
    /// Optional; absent on legacy/imports. The region facade projects this to an
    /// empty string when unset (see region contract task 033).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Classification of the region. Optional; unset regions project to
    /// <see cref="RegionType.Special"/> in the facade (task 033).
    /// </summary>
    public RegionType? Type { get; set; }

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
