using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(MerchantTradeService))]
public sealed class MerchantTradeService(MaritimeLedgerService ledger, MaritimeOptions options,
    ShipRegistryService registry, PhysicalShipService physical, ShipAccessService access, IslandService islands, ShipCrewService crew)
{
    public int MaximumQuantity => options.MaximumQuantity;
    public MaritimeEconomyState State => ledger.Snapshot();
    public IReadOnlyList<MerchantTradeOffer> GetPlayerOffers(string portId)
    {
        if (!State.Markets.TryGetValue(portId, out var market)) return [];
        return market.Goods.OrderBy(p => p.Key, StringComparer.Ordinal).SelectMany(p =>
        {
            var price = MaritimeMarketEngine.Prices(p.Value);
            return new MerchantTradeOffer[] { new(p.Key, price.Buy, true), new(p.Key, price.Sell, false) };
        }).ToArray();
    }
    public string DescribeMarket(string portId)
    {
        if (!State.Markets.TryGetValue(portId, out var market)) return "Market: unavailable (persistence or port configuration).";
        return string.Join(" | ", market.Goods.OrderBy(p => p.Key).Select(p =>
        { var price = MaritimeMarketEngine.Prices(p.Value); return $"{p.Key}: buy {price.Buy}, sell {price.Sell}, port stock {p.Value.Stock}"; }));
    }
    public string? MarketForIsland(string islandId) => State.Markets.Values.SingleOrDefault(m => m.IslandId == islandId)?.PortId;
    public bool AtMarket(ShipState source, string port, NwPlayer player)
    {
        if (!registry.IsCurrent(source) || source.IsInitializing || source.IsDocking || source.IsDisabled ||
            !access.CanCommand(source, player) || !player.IsValid || player.LoginCreature != player.ControlledCreature) return false;
        if (!State.Markets.TryGetValue(port, out var market)) return false;
        var island = islands.GetIslands().SingleOrDefault(i => i.Id == market.IslandId);
        if (island == null) return false;
        if (source.IsStored) return source.StoredPortId == island.Id &&
            string.Equals(player.ControlledCreature?.Area?.ResRef, island.LandingArea, StringComparison.OrdinalIgnoreCase);
        return source.HelmsmanPCKey == player.PlayerName && physical.IsPlayerPresent(source.ShipName, player) &&
            source.AreaResRef == island.OceanArea && MathF.Sqrt(MathF.Pow(source.X - island.OceanX, 2) + MathF.Pow(source.Y - island.OceanY, 2)) <= island.DockRadius;
    }
    public bool WithMerchant(ShipState source, ShipState merchant, string port, NwPlayer player) =>
        player.IsValid && player.LoginCreature is { IsValid: true, IsLoginPlayerCharacter: true } && player.LoginCreature == player.ControlledCreature &&
        registry.IsCurrent(source) && registry.IsCurrent(merchant) && ShipActionRules.CanTrade(source, merchant, port) &&
        source.HelmsmanPCKey == player.PlayerName && physical.IsPlayerPresent(source.ShipName, player) &&
        crew.GetRole(source.ShipName, player.PlayerName) == ShipCrewRole.Captain && access.CanCommand(source, player) && State.Markets.ContainsKey(port);
    public async Task<MaritimeResult> PlayerTradeAsync(NwPlayer player, ShipState source, ShipState? merchant,
        string port, string item, int quantity, bool buys)
    {
        if (quantity < 1 || quantity > options.MaximumQuantity) return MaritimeResult.Fail($"Quantity must be 1–{options.MaximumQuantity}.");
        if (player.LoginCreature != player.ControlledCreature) return MaritimeResult.Fail("Trade with your own login character.");
        var wallet = new MaritimeWallet(player);
        bool Valid() => merchant == null ? AtMarket(source, port, player) : WithMerchant(source, merchant, port, player);
        if (!Valid()) return MaritimeResult.Fail("The selected market or merchant is no longer available at your location.");
        ShipState[] participants = merchant == null ? [source] : [source, merchant];
        var result = await ledger.ExecuteAsync(participants, state =>
        {
            if (!state.Markets.TryGetValue(port, out var market)) throw new InvalidOperationException("No configured market.");
            MaritimeMarketEngine.Adjust(market, DateTime.UtcNow, options.MarketAdjustmentSeconds);
            var cargo = state.Vessels[source.ShipId];
            if (merchant == null) return MaritimeMarketEngine.PortTrade(state, cargo, source.CargoCapacity, port, item, quantity, buys, wallet.Gold, DateTime.UtcNow);
            return MerchantExchange(state, cargo, state.Vessels[merchant.ShipId], source.CargoCapacity, merchant.CargoCapacity,
                options.MerchantGoldLimit, port, item, quantity, buys, wallet.Gold, DateTime.UtcNow);
        }, Valid, wallet, merchant == null ? "port-trade" : "merchant-trade");
        return result.Success ? MaritimeResult.Ok($"{(buys ? "Bought" : "Sold")} {quantity} {item}. Cargo and payment saved. {result.Message}") : result;
    }
    public static int MerchantExchange(MaritimeEconomyState state, VesselEconomics player, VesselEconomics merchant,
        int playerCapacity, int merchantCapacity, int goldLimit, string port, string item, int quantity, bool buys, long gold, DateTime now)
    {
        if (quantity < 1 || !state.Markets.TryGetValue(port, out var market) || !market.Goods.TryGetValue(item, out var good))
            throw new InvalidOperationException("Invalid merchant commodity or quantity.");
        var prices = MaritimeMarketEngine.Prices(good);
        long total = (long)quantity * (buys ? prices.Buy : prices.Sell);
        var supplier = buys ? merchant : player;
        var receiver = buys ? player : merchant;
        int capacity = buys ? playerCapacity : merchantCapacity;
        if (quantity > MaritimeMarketEngine.Cargo(supplier, item))
            throw new InvalidOperationException(buys ? "Insufficient merchant stock for this quantity." : "Insufficient cargo aboard for this quantity.");
        if (receiver.Cargo.Sum(c => (long)c.Quantity) + quantity > capacity)
            throw new InvalidOperationException(buys ? "Your ship's cargo hold is full for this quantity." : "The merchant's cargo hold is full for this quantity.");
        if (total > int.MaxValue || total <= 0 || buys && (gold < total || merchant.MerchantGold + total > goldLimit) ||
            !buys && (merchant.MerchantGold < total || gold + total > uint.MaxValue))
            throw new InvalidOperationException("Insufficient gold or merchant treasury limit.");
        MaritimeMarketEngine.AddCargo(supplier, item, -quantity); MaritimeMarketEngine.AddCargo(receiver, item, quantity);
        merchant.MerchantGold += buys ? (int)total : -(int)total;
        state.RecentTrades.Add(new() { At = now, PortId = port, ShipId = player.ShipId, Commodity = item,
            Quantity = quantity, Gold = (int)total, ShipBuys = buys, Counterparty = merchant.ShipId });
        MaritimeMarketEngine.TrimHistory(state);
        return buys ? -(int)total : (int)total;
    }
    /// <summary>Once-only arrival ledger. Purchases are selected for the next existing route destination.</summary>
    public static bool NpcArrival(MaritimeEconomyState state, ShipState ship, string port, string? nextPort,
        long arrival, DateTime now, MaritimeOptions options)
    {
        if (!state.Markets.TryGetValue(port, out var market)) throw new InvalidOperationException("Missing configured arrival market.");
        var vessel = state.Vessels[ship.ShipId];
        if (vessel.ArrivalSequence >= arrival) return false;
        MaritimeMarketEngine.Adjust(market, now, options.MarketAdjustmentSeconds);
        state.Markets.TryGetValue(nextPort ?? "", out var next);
        foreach (var cargo in vessel.Cargo.ToArray().OrderBy(c => c.ItemId))
        {
            if (!market.Goods.TryGetValue(cargo.ItemId, out var local)) continue;
            int price = MaritimeMarketEngine.Prices(local).Sell;
            if (next?.Goods.TryGetValue(cargo.ItemId, out var destination) == true && MaritimeMarketEngine.Prices(destination).Sell > price) continue;
            int quantity = Math.Min(cargo.Quantity, Math.Min(local.MaximumStock - local.Stock, market.Treasury / Math.Max(1, price)));
            quantity = Math.Min(quantity, Math.Max(0, (options.MerchantGoldLimit - vessel.MerchantGold) / Math.Max(1, price)));
            while (quantity > 0)
            {
                try { vessel.MerchantGold += MaritimeMarketEngine.PortTrade(state, vessel, ship.CargoCapacity, port, cargo.ItemId, quantity, false, vessel.MerchantGold, now); break; }
                catch (InvalidOperationException) { quantity--; }
            }
        }
        if (next != null)
            foreach (var pair in market.Goods.OrderByDescending(p => next.Goods.TryGetValue(p.Key, out var future)
                ? MaritimeMarketEngine.Prices(future).Sell - MaritimeMarketEngine.Prices(p.Value).Buy : int.MinValue).ThenBy(p => p.Key))
            {
                if (!next.Goods.TryGetValue(pair.Key, out var future) || MaritimeMarketEngine.Prices(future).Sell <= MaritimeMarketEngine.Prices(pair.Value).Buy) continue;
                int quantity = Math.Min(options.MerchantPurchaseQuantity, Math.Min(pair.Value.Stock,
                    Math.Max(0, ship.CargoCapacity - (int)vessel.Cargo.Sum(c => (long)c.Quantity))));
                while (quantity > 0 && MaritimeMarketEngine.Prices(pair.Value, pair.Value.Stock - quantity + 1).Buy >= MaritimeMarketEngine.Prices(future, Math.Min(future.MaximumStock, future.Stock + quantity)).Sell) quantity--;
                while (quantity > 0)
                {
                    try { vessel.MerchantGold += MaritimeMarketEngine.PortTrade(state, vessel, ship.CargoCapacity, port, pair.Key, quantity, true, vessel.MerchantGold, now); break; }
                    catch (InvalidOperationException) { quantity--; }
                }
            }
        vessel.ArrivalSequence = arrival; vessel.ArrivalPort = port; vessel.Departure = ship.PortDepartureTime;
        return true;
    }
    public Task<MaritimeResult> ProcessArrivalAsync(ShipState ship, string port, string? nextPort, long arrival) =>
        ledger.ExecuteAsync([ship], state => { NpcArrival(state, ship, port, nextPort, arrival, DateTime.UtcNow, options); return 0; },
            () => registry.IsCurrent(ship) && ship.ShipType == ShipType.Merchant && ship.IsActiveWorld && !ship.IsDisabled && ship.IsInPort && ship.CurrentTradePortId == port,
            kind: "npc-arrival");
}
