using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Persistence;

/// <summary>
/// Maps between <see cref="RegionDefinition"/> domain objects and
/// <see cref="PersistedRegionDefinition"/> EF entities.
/// </summary>
public static class RegionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PersistedRegionDefinition ToEntity(RegionDefinition definition)
    {
        return new PersistedRegionDefinition
        {
            Tag = definition.Tag.Value,
            Name = definition.Name,
            DefaultChaosJson = definition.DefaultChaos != null
                ? JsonSerializer.Serialize(definition.DefaultChaos, JsonOptions)
                : null,
            AreasJson = JsonSerializer.Serialize(
                definition.Areas.Select(ToAreaDto).ToList(), JsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static RegionDefinition ToDomain(PersistedRegionDefinition entity)
    {
        ChaosState? defaultChaos = null;
        if (!string.IsNullOrEmpty(entity.DefaultChaosJson))
        {
            defaultChaos = JsonSerializer.Deserialize<ChaosState>(entity.DefaultChaosJson, JsonOptions);
        }

        var areaDtos = JsonSerializer.Deserialize<List<AreaJsonDto>>(entity.AreasJson, JsonOptions)
                       ?? new List<AreaJsonDto>();

        return new RegionDefinition
        {
            Tag = new RegionTag(entity.Tag),
            Name = entity.Name,
            DefaultChaos = defaultChaos,
            Areas = areaDtos.Select(FromAreaDto).ToList()
        };
    }

    public static void UpdateEntity(PersistedRegionDefinition entity, RegionDefinition definition)
    {
        entity.Name = definition.Name;
        entity.DefaultChaosJson = definition.DefaultChaos != null
            ? JsonSerializer.Serialize(definition.DefaultChaos, JsonOptions)
            : null;
        entity.AreasJson = JsonSerializer.Serialize(
            definition.Areas.Select(ToAreaDto).ToList(), JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    // ==================== Internal DTOs for JSON serialization ====================

    private static AreaJsonDto ToAreaDto(AreaDefinition area)
    {
        return new AreaJsonDto
        {
            ResRef = area.ResRef.Value,
            DefinitionTags = area.DefinitionTags,
            LinkedSettlement = area.LinkedSettlement?.Value,
            Environment = new EnvironmentJsonDto
            {
                Climate = area.Environment.Climate.ToString(),
                SoilQuality = area.Environment.SoilQuality.ToString(),
                MineralQualityRange = new QualityRangeJsonDto
                {
                    Min = area.Environment.MineralQualityRange.Min.ToString(),
                    Max = area.Environment.MineralQualityRange.Max.ToString()
                },
                Chaos = area.Environment.Chaos
            },
            PlacesOfInterest = area.PlacesOfInterest?.Select(p => new PoiJsonDto
            {
                ResRef = p.ResRef,
                Tag = p.Tag,
                Name = p.Name,
                Type = p.Type.ToString(),
                Description = p.Description
            }).ToList()
        };
    }

    private static AreaDefinition FromAreaDto(AreaJsonDto dto)
    {
        Enum.TryParse<Climate>(dto.Environment?.Climate, true, out var climate);
        Enum.TryParse<EconomyQuality>(dto.Environment?.SoilQuality, true, out var soilQuality);

        Enum.TryParse<EconomyQuality>(dto.Environment?.MineralQualityRange?.Min, true, out var minQuality);
        Enum.TryParse<EconomyQuality>(dto.Environment?.MineralQualityRange?.Max, true, out var maxQuality);
        if (minQuality == default) minQuality = EconomyQuality.Average;
        if (maxQuality == default) maxQuality = EconomyQuality.Average;

        var env = new EnvironmentData(climate, soilQuality,
            new QualityRange(minQuality, maxQuality), dto.Environment?.Chaos);

        List<PlaceOfInterest>? pois = dto.PlacesOfInterest?.Select(p =>
        {
            Enum.TryParse<PoiType>(p.Type, true, out var poiType);
            return new PlaceOfInterest(p.ResRef, p.Tag, p.Name, poiType, p.Description);
        }).ToList();

        SettlementId? settlement = dto.LinkedSettlement is > 0
            ? SettlementId.Parse(dto.LinkedSettlement.Value)
            : null;

        return new AreaDefinition(
            new AreaTag(dto.ResRef),
            dto.DefinitionTags ?? new List<string>(),
            env,
            pois,
            settlement);
    }

    // JSON DTOs — these are only used internally for JSONB serialization, not exposed externally
    private class AreaJsonDto
    {
        public string ResRef { get; set; } = string.Empty;
        public List<string>? DefinitionTags { get; set; }
        public int? LinkedSettlement { get; set; }
        public EnvironmentJsonDto? Environment { get; set; }
        public List<PoiJsonDto>? PlacesOfInterest { get; set; }
    }

    private class EnvironmentJsonDto
    {
        public string? Climate { get; set; }
        public string? SoilQuality { get; set; }
        public QualityRangeJsonDto? MineralQualityRange { get; set; }
        public ChaosState? Chaos { get; set; }
    }

    private class QualityRangeJsonDto
    {
        public string? Min { get; set; }
        public string? Max { get; set; }
    }

    private class PoiJsonDto
    {
        public string ResRef { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Description { get; set; }
    }
}
