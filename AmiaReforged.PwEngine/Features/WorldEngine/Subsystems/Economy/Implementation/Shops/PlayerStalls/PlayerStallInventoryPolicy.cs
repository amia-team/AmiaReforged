using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Defines which player-held items may be listed on a stall.
/// </summary>
internal static class PlayerStallInventoryPolicy
{
    private static readonly HashSet<string> ResRefBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "ds_pckey",
        "utilitytoken",
        "utility_token_pactfeat",
        "ds_dicebag",
        "pc_dcrod",
        "ds_party_item",
        "ds_pvp_tool",
        "dmfi_pc_emote2",
        "jj_asn_tool",
        "umbran_arts_1",
        "umbran_arts_2",
        "umbran_arts_3",
        "umbran_arts_4",
        "umbran_arts_5",
        "umbran_arts_6",
        "umbran_arts_7",
        "umbran_arts_8",
        "barb_rages",
        "alarmsong",
        "firesong",
        "icesong",
        "warsong",

    };

    /// <summary>
    /// Returns <c>true</c> when the supplied item may be listed for sale.
    /// </summary>
    public static bool IsItemAllowed(NwItem item)
    {
        if (item is not { IsValid: true })
        {
            return false;
        }

        string resRef = item.ResRef?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resRef))
        {
            return false;
        }

        return !ResRefBlacklist.Contains(resRef) || item.PlotFlag;
    }
}
