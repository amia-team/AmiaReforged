using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Regions;

public record AreaDefinition(string ResRef, List<string> DefinitionTags, EnvironmentData Environment);

public record EnvironmentData(Climate Climate, EconomyQuality SoilQuality, QualityRange MineralQualityRange);

public record QualityRange(EconomyQuality Min = EconomyQuality.Average, EconomyQuality Max = EconomyQuality.Average);
