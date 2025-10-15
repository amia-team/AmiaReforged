using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Items;

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemDefinition definition);
    ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag);
    List<ItemDefinition> AllItems();
}
