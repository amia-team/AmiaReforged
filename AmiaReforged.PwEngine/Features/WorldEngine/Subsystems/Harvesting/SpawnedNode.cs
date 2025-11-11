using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

public record SpawnedNode(NwPlaceable? Placeable, ResourceNodeInstance Instance);
