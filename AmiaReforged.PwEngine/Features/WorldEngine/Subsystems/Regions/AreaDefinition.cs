using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;

public record AreaDefinition(
    AreaTag ResRef,
    List<string> DefinitionTags,
    EnvironmentData Environment,
    List<PlaceOfInterest>? PlacesOfInterest = null,
    SettlementId? LinkedSettlement = null);


public record EnvironmentData(
    Climate Climate,
    EconomyQuality SoilQuality,
    QualityRange MineralQualityRange,
    ChaosState? Chaos = null);

public record QualityRange(EconomyQuality Min = EconomyQuality.Average, EconomyQuality Max = EconomyQuality.Average);

public record PlaceOfInterest(string ResRef, string Tag, string Name, PoiType Type, string? Description = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PoiType
{
    Undefined,
    Dungeon,
    Landmark,
    ResourceNode,
    House,
    Guild,
    Temple,
    Library,
    Shop,
    Warehouse,
    Bank
}
