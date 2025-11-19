namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemData.ItemBlueprint definition);
    ItemData.ItemBlueprint? GetByTag(string harvestOutputItemDefinitionTag);
    ItemData.ItemBlueprint? GetByResRef(string resRef);
    List<ItemData.ItemBlueprint> AllItems();
}
