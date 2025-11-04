using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Moves lapsed stall inventory into the custody of the area's market reeve NPC.
/// </summary>
[ServiceBinding(typeof(IPlayerStallInventoryCustodian))]
internal sealed class MarketReeveStallInventoryCustodian : IPlayerStallInventoryCustodian
{
    private const string MarketReeveTag = "market_reeve";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;

    public MarketReeveStallInventoryCustodian(IPlayerShopRepository shops)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
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

        NwCreature? reeve = await LocateMarketReeveAsync(stall.AreaResRef, cancellationToken).ConfigureAwait(false);
        if (reeve is null)
        {
            Log.Warn(
                "Suspended stall {StallId} inventory could not be transferred because no market reeve was found in area '{AreaResRef}'.",
                stall.Id,
                stall.AreaResRef);
            return;
        }

        foreach (StallProduct product in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int quantity = Math.Max(1, product.Quantity);
            int delivered = 0;

            for (int index = 0; index < quantity; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                NwItem? restored = await StallProductRestorer.RestoreItemAsync(product, reeve).ConfigureAwait(false);
                if (restored is null)
                {
                    continue;
                }

                delivered++;
                ApplyItemMetadata(restored, stall, product);
            }

            _shops.RemoveProductFromShop(stall.Id, product.Id);

            Log.Info(
                "Transferred {Count} copies of stall product {ProductId} from stall {StallId} to market reeve.",
                delivered,
                product.Id,
                stall.Id);
        }
    }

    private static async Task<NwCreature?> LocateMarketReeveAsync(string areaResRef, CancellationToken cancellationToken)
    {
        await NwTask.SwitchToMainThread();

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(areaResRef))
        {
            return null;
        }

        NwArea? area = NwModule.Instance.Areas
            .FirstOrDefault(candidate => string.Equals(candidate.ResRef?.ToString(), areaResRef, StringComparison.OrdinalIgnoreCase));

        if (area is null)
        {
            return null;
        }

        return NwObject.FindObjectsWithTag<NwCreature>(MarketReeveTag)
            .FirstOrDefault(creature => creature is { IsValid: true } && creature.Area == area);
    }

    private static void ApplyItemMetadata(NwItem item, PlayerStall stall, StallProduct product)
    {
        string? personaId = ChoosePersonaId(product, stall);
        if (!string.IsNullOrWhiteSpace(personaId))
        {
            NWScript.SetLocalString(item, PlayerStallItemLocals.ConsignorPersonaId, personaId);
        }

        NWScript.SetLocalString(item, PlayerStallItemLocals.SourceStallId, stall.Id.ToString(CultureInfo.InvariantCulture));
        NWScript.SetLocalString(item, PlayerStallItemLocals.SourceProductId, product.Id.ToString(CultureInfo.InvariantCulture));
    }

    private static string? ChoosePersonaId(StallProduct product, PlayerStall stall)
    {
        if (!string.IsNullOrWhiteSpace(product.ConsignedByPersonaId))
        {
            return product.ConsignedByPersonaId;
        }

        return string.IsNullOrWhiteSpace(stall.OwnerPersonaId) ? null : stall.OwnerPersonaId;
    }
}
