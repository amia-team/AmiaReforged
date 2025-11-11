using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

public record ItemDto(Items.ItemData.ItemDefinition BaseDefinition, IPQuality Quality, IPQuality Quantity);
