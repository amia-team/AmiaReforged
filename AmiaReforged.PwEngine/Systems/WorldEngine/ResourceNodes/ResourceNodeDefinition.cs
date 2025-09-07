using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public record ResourceNodeDefinition(int PlcAppearance, NodeType Type, string Tag, HarvestContext Requirement, HarvestOutput[] Outputs, int BaseHarvestRounds = 0);

public enum NodeType
{
    Ore,
    Tree,
    Boulder,
    Geode,
    Corpse,
    Excavation,
    Flora,
    Misc,
    Unknown
}
