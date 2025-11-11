using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

public record HarvestContext(JobSystemItemType RequiredItemType, MaterialEnum RequiredItemMaterial = MaterialEnum.None);
