using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

public record ItemDto(Items.ItemData.ItemBlueprint BaseDefinition, IPQuality Quality, IPQuality Quantity);
