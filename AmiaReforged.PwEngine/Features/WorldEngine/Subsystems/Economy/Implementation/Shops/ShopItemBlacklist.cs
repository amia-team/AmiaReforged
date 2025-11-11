using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

[ServiceBinding(typeof(IShopItemBlacklist))]
public sealed class ShopItemBlacklist : IShopItemBlacklist
{
    private readonly HashSet<string> _blacklistedResRefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "dm_tool_admin",
        "system_debug_wand"
    };

    public bool IsBlacklisted(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef))
        {
            return true;
        }

        return _blacklistedResRefs.Contains(resRef);
    }

    public void Register(IEnumerable<string> resRefs)
    {
        if (resRefs == null)
        {
            return;
        }

        foreach (string resRef in resRefs)
        {
            if (!string.IsNullOrWhiteSpace(resRef))
            {
                _blacklistedResRefs.Add(resRef);
            }
        }
    }
}
