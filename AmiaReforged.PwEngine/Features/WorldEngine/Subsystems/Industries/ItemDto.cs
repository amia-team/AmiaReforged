using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public record ItemDto(Items.ItemData.ItemBlueprint BaseDefinition, IPQuality Quality, IPQuality Quantity);
