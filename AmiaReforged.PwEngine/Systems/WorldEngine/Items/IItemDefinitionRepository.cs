namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public interface IItemDefinitionRepository
{
    void AddItemDefinition(ItemDefinition definition);
    ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag);
}
