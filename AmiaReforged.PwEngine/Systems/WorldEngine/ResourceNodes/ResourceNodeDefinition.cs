using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public record ResourceNodeDefinition(int PlcAppearance, NodeType Type, string Tag, HarvestContext Requirement, HarvestOutput[] Outputs, int BaseHarvestRounds = 0);

public enum NodeType
{
    Ore = 0,
    Tree = 1,
    Boulder = 2,
    Geode = 3,
    Corpse = 4,
    Excavation = 5,
    Flora = 6,
    Misc = 7,
    Unknown = -1
}
