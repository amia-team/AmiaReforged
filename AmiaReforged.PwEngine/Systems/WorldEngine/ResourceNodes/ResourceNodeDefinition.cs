using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public record ResourceNodeDefinition(int PlcAppearance, ResourceType Type, string Tag, HarvestContext Requirement, HarvestOutput[] Outputs, int Uses = 50, int BaseHarvestRounds = 0);
public record ResourceNodeDefinition(int PlcAppearance, ResourceType Type, string Tag, HarvestContext Requirement, HarvestOutput[] Outputs, int BaseHarvestRounds = 0);
