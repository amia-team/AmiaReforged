using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

[ServiceBinding(typeof(BoonFactory))]
public class BoonFactory
{
    private readonly Dictionary<BoonType, IBoon> _boons;

    public BoonFactory(IEnumerable<IBoon> boons)
        => _boons = boons.ToDictionary(b => b.BoonType);

    public IBoon? GetBoon(BoonType boonType) => _boons.GetValueOrDefault(boonType);
}
