using System;
using System.Collections.Generic;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;

/// <summary>
/// Default blacklist for bank storage, preventing account-critical items from being stashed.
/// </summary>
[ServiceBinding(typeof(IBankStorageItemBlacklist))]
public sealed class BankStorageItemBlacklist : IBankStorageItemBlacklist
{
    private static readonly string[] DefaultBlockedResrefs =
    {
        "ds_pckey",
        "amia_premium_token",
        "ds_party_item",
        "ds_pvp_tool",
        "dmfi_pc_emote2",
        "ds_dicebag",
        "pc_dcrod",
        "bank_deed",
    };

    private readonly HashSet<string> _blockedResrefs = new(DefaultBlockedResrefs, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> BlacklistedResrefs { get; } = Array.AsReadOnly(DefaultBlockedResrefs);

    public bool IsBlacklisted(string? resref)
    {
        if (string.IsNullOrWhiteSpace(resref))
        {
            return false;
        }

        return _blockedResrefs.Contains(resref);
    }
}
