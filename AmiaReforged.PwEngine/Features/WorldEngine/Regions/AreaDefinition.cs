using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

public record AreaDefinition(string ResRef, List<string> DefinitionTags, EnvironmentData Environment, SiteData? PlaceOfInterest = null);

public record SiteData(string Tag, string Name);

public record EnvironmentData(Climate Climate, EconomyQuality SoilQuality, QualityRange MineralQualityRange);

public record QualityRange(EconomyQuality Min = EconomyQuality.Average, EconomyQuality Max = EconomyQuality.Average);
