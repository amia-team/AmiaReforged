namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly Dictionary<string, ItemDefinition> _itemDefinitions = new();

    public void AddItemDefinition(ItemDefinition definition)
    {
        _itemDefinitions.TryAdd(definition.ItemTag, definition);
    }

    public ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag)
    {
        return _itemDefinitions.GetValueOrDefault(harvestOutputItemDefinitionTag);
    }
}
