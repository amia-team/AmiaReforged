using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;

public record HarvestContext(JobSystemItemType RequiredItemType, MaterialEnum RequiredItemMaterial = MaterialEnum.None);
