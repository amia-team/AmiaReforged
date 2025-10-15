using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries;

public record ItemDto(ItemDefinition BaseDefinition, IPQuality Quality, IPQuality Quantity);
