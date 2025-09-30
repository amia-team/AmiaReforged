using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemDefinition definition);
    ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag);
    List<ItemDefinition> AllItems();
}
