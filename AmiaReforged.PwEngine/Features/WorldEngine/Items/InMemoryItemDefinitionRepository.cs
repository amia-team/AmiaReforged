using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Items;

[ServiceBinding(typeof(IItemDefinitionRepository))]
public class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly Dictionary<string, ItemDefinition> _itemDefinitions = new();

    public void AddItemDefinition(ItemDefinition definition)
    {
        bool added = _itemDefinitions.TryAdd(definition.ItemTag, definition);

        if (!added)
        {
            _itemDefinitions[definition.ItemTag] = definition;
        }
    }

    public ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag)
    {
        return _itemDefinitions.GetValueOrDefault(harvestOutputItemDefinitionTag);
    }

    public List<ItemDefinition> AllItems()
    {
        return _itemDefinitions.Values.ToList();
    }
}
