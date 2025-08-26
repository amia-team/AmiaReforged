using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public record ResourceNodeDefinition(string Tag, HarvestContext[] Requirements, HarvestOutput[] Outputs);
