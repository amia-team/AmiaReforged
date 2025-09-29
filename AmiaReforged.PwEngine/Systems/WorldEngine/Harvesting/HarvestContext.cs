using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public record HarvestContext(JobSystemItemType RequiredItemType, MaterialEnum RequiredItemMaterial = MaterialEnum.None);