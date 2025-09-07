using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public record HarvestOutput(string ItemDefinitionTag, int Quantity, int Chance = 100);
