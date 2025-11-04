using System;
using System.Collections.Generic;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Defines which player-held items may be listed on a stall.
/// </summary>
internal static class PlayerStallInventoryPolicy
{
    private static readonly HashSet<string> ResRefBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "ds_pckey",
        "utilitytoken",
        "utility_token_pactfeat"
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

        return !ResRefBlacklist.Contains(resRef);
    }
}
