using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

[ServiceBinding(typeof(IItemDefinitionRepository))]
public class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly Dictionary<string, ItemData.ItemBlueprint> _itemDefinitions = new();

    public void AddItemDefinition(ItemData.ItemBlueprint definition)
    {
        bool added = _itemDefinitions.TryAdd(definition.ItemTag, definition);

        if (!added)
        {
            _itemDefinitions[definition.ItemTag] = definition;
        }
    }

    public ItemData.ItemBlueprint? GetByTag(string harvestOutputItemDefinitionTag)
    {
        return _itemDefinitions.GetValueOrDefault(harvestOutputItemDefinitionTag);
    }

    public ItemData.ItemBlueprint? GetByResRef(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef)) return null;
        return _itemDefinitions.Values.FirstOrDefault(d => string.Equals(d.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
    }

    public List<ItemData.ItemBlueprint> AllItems()
    {
        return _itemDefinitions.Values.ToList();
    }
}
