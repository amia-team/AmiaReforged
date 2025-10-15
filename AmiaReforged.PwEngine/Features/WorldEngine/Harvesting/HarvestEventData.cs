using AmiaReforged.PwEngine.Features.WorldEngine.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;

public record HarvestEventData(ICharacter Character, ResourceNodeInstance NodeInstance);
