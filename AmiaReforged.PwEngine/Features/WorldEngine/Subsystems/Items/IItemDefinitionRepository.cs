namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemData.ItemDefinition definition);
    ItemData.ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag);
    ItemData.ItemDefinition? GetByResRef(string resRef);
    List<ItemData.ItemDefinition> AllItems();
}
