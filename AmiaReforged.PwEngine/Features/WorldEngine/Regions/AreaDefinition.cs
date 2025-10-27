using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

public record AreaDefinition(AreaTag ResRef, List<string> DefinitionTags, EnvironmentData Environment, SiteData? PlaceOfInterest = null);

public record SiteData(string Tag, string Name);

public record EnvironmentData(Climate Climate, EconomyQuality SoilQuality, QualityRange MineralQualityRange);

public record QualityRange(EconomyQuality Min = EconomyQuality.Average, EconomyQuality Max = EconomyQuality.Average);
