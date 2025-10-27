using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

[ServiceBinding(typeof(IRegionRepository))]
public class InMemoryRegionRepository : IRegionRepository
{
    private readonly Dictionary<string, RegionDefinition> _regions = new();
    private readonly Dictionary<int, string> _settlementToRegionTag = new();

    public void Add(RegionDefinition definition)
    {
        bool added = _regions.TryAdd(definition.Tag, definition);

        if (!added)
        {
            _regions[definition.Tag] = definition;
        }

        IndexSettlements(definition);
    }

    public void Update(RegionDefinition definition)
    {
        if (!Exists(definition.Tag))
        {
            Add(definition);
            return;
        }

        _regions[definition.Tag] = definition;
        IndexSettlements(definition);
    }

    public bool Exists(string tag)
    {
        return _regions.ContainsKey(tag);
    }

    public List<RegionDefinition> All()
    {
        return _regions.Values.ToList();
    }

    public bool TryGetRegionBySettlement(int settlementId, out RegionDefinition? region)
    {
        region = null;
        if (_settlementToRegionTag.TryGetValue(settlementId, out string? tag) && _regions.TryGetValue(tag, out RegionDefinition? reg))
        {
            region = reg;
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<int> GetSettlements(string regionTag)
    {
        return _regions.TryGetValue(regionTag, out RegionDefinition? region)
            ? region.Settlements
            : Array.Empty<int>();
    }

    private void IndexSettlements(RegionDefinition definition)
    {
        if (definition.Settlements.Count == 0)
            return;

        foreach (int sid in definition.Settlements)
        {
            _settlementToRegionTag[sid] = definition.Tag;
        }
    }
}
