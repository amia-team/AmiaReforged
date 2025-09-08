using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Domains;

[ServiceBinding(typeof(IRegionRepository))]
public class InMemoryRegionRepository : IRegionRepository
{
    private readonly Dictionary<string, RegionDefinition> _regions = new();

    public void Add(RegionDefinition definition)
    {
        _regions.TryAdd(definition.Tag, definition);
    }

    public void Update(RegionDefinition definition)
    {
        if (!Exists(definition.Tag))
        {
            Add(definition);
            return;
        }

        _regions[definition.Tag] = definition;
    }

    public bool Exists(string tag)
    {
        return _regions.ContainsKey(tag);
    }

    public List<RegionDefinition> All()
    {
        return _regions.Values.ToList();
    }
}
