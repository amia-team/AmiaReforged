using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public record HarvestContext(JobSystemItemType RequiredItemType, MaterialEnum RequiredItemMaterial = MaterialEnum.None);