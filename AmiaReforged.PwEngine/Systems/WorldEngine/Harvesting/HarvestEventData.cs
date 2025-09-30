using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public record HarvestEventData(ICharacter Character, ResourceNodeInstance NodeInstance);
