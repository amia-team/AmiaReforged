using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Moves lapsed stall inventory into the custody of the area's market reeve NPC.
/// </summary>
[ServiceBinding(typeof(IPlayerStallInventoryCustodian))]
internal sealed class MarketReeveStallInventoryCustodian : IPlayerStallInventoryCustodian
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly ReeveLockupService _lockup;

    public MarketReeveStallInventoryCustodian(IPlayerShopRepository shops, ReeveLockupService lockup)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _lockup = lockup ?? throw new ArgumentNullException(nameof(lockup));
    }

    public async Task TransferInventoryToMarketReeveAsync(PlayerStall stall, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stall);

        List<StallProduct>? products = _shops.ProductsForShop(stall.Id);
        if (products is null || products.Count == 0)
        {
            return;
        }

        List<StallProduct> pending = products
            .Where(product => product.Quantity > 0)
            .ToList();

        if (pending.Count == 0)
        {
            return;
        }

        int stored = await _lockup.StoreSuspendedInventoryAsync(stall, pending, cancellationToken).ConfigureAwait(false);

        int removedProducts = 0;

        foreach (StallProduct product in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int quantity = Math.Max(1, product.Quantity);
            _shops.RemoveProductFromShop(stall.Id, product.Id);
            removedProducts++;

            Log.Info(
                "Transferred {Count} copies of stall product {ProductId} from stall {StallId} into market reeve lockup storage.",
                quantity,
                product.Id,
                stall.Id);
        }

        Log.Info(
            "Completed reeve transfer for stall {StallId}: {ProductCount} listings removed, {ItemCount} items stored.",
            stall.Id,
            removedProducts,
            stored);
    }
}
