using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Harvesting;

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
