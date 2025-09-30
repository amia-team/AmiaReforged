using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public record ItemDto(ItemDefinition BaseDefinition, IPQuality Quality, IPQuality Quantity);
