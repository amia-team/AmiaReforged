using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

[ServiceBinding(typeof(IItemDefinitionRepository))]
public class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly Dictionary<string, ItemData.ItemDefinition> _itemDefinitions = new();

    public void AddItemDefinition(ItemData.ItemDefinition definition)
    {
        bool added = _itemDefinitions.TryAdd(definition.ItemTag, definition);

        if (!added)
        {
            _itemDefinitions[definition.ItemTag] = definition;
        }
    }

    public ItemData.ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag)
    {
        return _itemDefinitions.GetValueOrDefault(harvestOutputItemDefinitionTag);
    }

    public ItemData.ItemDefinition? GetByResRef(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef)) return null;
        return _itemDefinitions.Values.FirstOrDefault(d => string.Equals(d.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
    }

    public List<ItemData.ItemDefinition> AllItems()
    {
        return _itemDefinitions.Values.ToList();
    }
}
