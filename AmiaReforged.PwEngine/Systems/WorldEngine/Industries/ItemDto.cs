using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public record ItemDto(ItemDefinition BaseDefinition, IPQuality Quality, int Quantity);