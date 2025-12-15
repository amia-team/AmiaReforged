using Anvil.API;
using AmiaReforged.PwEngine.Features.AI.Core.Models;

namespace AmiaReforged.PwEngine.Features.AI.Core.Interfaces;

/// <summary>
/// Interface for managing loot bin containers.
/// Ported from InitialiseLootBin(), GetLootBin() in inc_ds_ondeath.nss.
/// </summary>
public interface ILootBinManager
{
    /// <summary>
    /// Gets the appropriate loot bin for a creature based on its CR and settings.
    /// </summary>
    /// <param name="creature">The creature to get a loot bin for.</param>
    /// <returns>The loot bin container, or null if not found.</returns>
    NwPlaceable? GetLootBin(NwCreature creature);

    /// <summary>
    /// Gets a loot bin by tier.
    /// </summary>
    /// <param name="tier">The loot tier.</param>
    /// <returns>The loot bin container, or null if not found.</returns>
    NwPlaceable? GetLootBinByTier(LootTier tier);

    /// <summary>
    /// Gets a loot bin by custom tag.
    /// </summary>
    /// <param name="tag">The loot bin tag.</param>
    /// <returns>The loot bin container, or null if not found.</returns>
    NwPlaceable? GetLootBinByTag(string tag);

    /// <summary>
    /// Gets a random item from a loot bin.
    /// </summary>
    /// <param name="lootBin">The loot bin to pick from.</param>
    /// <returns>A copy of a random item from the bin, or null if empty.</returns>
    NwItem? GetRandomItemFromBin(NwPlaceable lootBin);

    /// <summary>
    /// Initializes a loot bin (indexes items for random selection).
    /// </summary>
    /// <param name="container">The container to initialize.</param>
    /// <param name="force">Force re-initialization even if already done.</param>
    void InitializeLootBin(NwPlaceable container, bool force = false);
}
