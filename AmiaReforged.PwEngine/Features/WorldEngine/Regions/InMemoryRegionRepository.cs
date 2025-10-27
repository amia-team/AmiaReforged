using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

[ServiceBinding(typeof(IRegionRepository))]
public class InMemoryRegionRepository : IRegionRepository
{
    private readonly Dictionary<string, RegionDefinition> _regions = new();
    private readonly Dictionary<int, string> _settlementToRegionTag = new();

    public void Add(RegionDefinition definition)
    {
        // Implicit conversion from RegionTag to string
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

    public bool Exists(RegionTag tag)
    {
        // Implicit conversion from RegionTag to string
        return _regions.ContainsKey(tag);
    }

    public List<RegionDefinition> All()
    {
        return _regions.Values.ToList();
    }

    public bool TryGetRegionBySettlement(SettlementId settlementId, out RegionDefinition? region)
    {
        region = null;
        // Implicit conversion from SettlementId to int
        if (_settlementToRegionTag.TryGetValue(settlementId, out string? tag) && _regions.TryGetValue(tag, out RegionDefinition? reg))
        {
            region = reg;
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<SettlementId> GetSettlements(RegionTag regionTag)
    {
        // Implicit conversion from RegionTag to string
        return _regions.TryGetValue(regionTag, out RegionDefinition? region)
            ? region.Settlements
            : Array.Empty<SettlementId>();
    }

    public void Clear()
    {
        _regions.Clear();
        _settlementToRegionTag.Clear();
    }

    private void IndexSettlements(RegionDefinition definition)
    {
        if (definition.Settlements.Count == 0)
            return;

        foreach (SettlementId sid in definition.Settlements)
        {
            // Implicit conversion from SettlementId to int and RegionTag to string
            _settlementToRegionTag[sid] = definition.Tag;
        }
    }
}
