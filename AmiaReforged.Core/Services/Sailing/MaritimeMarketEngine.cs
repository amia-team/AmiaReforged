using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Pure, deterministic stock/price/cargo accounting. No native objects or I/O.</summary>
public static class MaritimeMarketEngine
{
    public static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    public static void Validate(MaritimeEconomyState state)
    {
        if (state.Version != 1 || state.Revision < 0 || state.Markets == null || state.Vessels == null ||
            state.Rentals == null || state.RecentTrades == null || state.PendingMoney == null)
            throw new InvalidDataException("Invalid maritime state; existing data must not be replaced.");
        foreach (var pair in state.Markets)
        {
            MaritimeMarket market = pair.Value;
            if (market == null || pair.Key != market.PortId || string.IsNullOrWhiteSpace(market.IslandId) ||
                market.Treasury < 0 || market.Treasury > market.MaximumTreasury || market.Goods == null || market.Goods.Count == 0)
                throw new InvalidDataException("Invalid port market.");
            foreach (var entry in market.Goods)
            {
                MarketCommodity g = entry.Value;
                if (g == null || entry.Key != g.Id || g.Floor < 1 || g.Spread < 1 || g.Ceiling > 1000000 || g.Ceiling <= (long)g.Floor + g.Spread ||
                    g.BasePrice < g.Floor || g.BasePrice > g.Ceiling || g.UnitsPerPriceStep < 1 || g.MaximumStock < 1 ||
                    g.Stock < 0 || g.Stock > g.MaximumStock || g.TargetStock < 0 || g.TargetStock > g.MaximumStock)
                    throw new InvalidDataException("Invalid commodity stock/pricing.");
            }
        }
        foreach (var pair in state.Vessels)
            if (pair.Value == null || pair.Key != pair.Value.ShipId || pair.Value.Cargo == null || pair.Value.MerchantGold < 0 ||
                pair.Value.ArrivalSequence < 0 || pair.Value.RentalStorageRevision < 0 || pair.Value.RentalStorageRevision > state.Revision ||
                (pair.Value.RentalStorageRevision > 0 && (string.IsNullOrWhiteSpace(pair.Value.RentalArea) ||
                    !float.IsFinite(pair.Value.RentalX) || !float.IsFinite(pair.Value.RentalY) || !float.IsFinite(pair.Value.RentalZ) ||
                    !Enum.IsDefined(pair.Value.RentalHeading) || pair.Value.RentalHull < 0 ||
                    pair.Value.RentalStored && string.IsNullOrWhiteSpace(pair.Value.RentalStoredPort))) || pair.Value.Cargo.Any(c => c == null || string.IsNullOrWhiteSpace(c.ItemId) || c.Quantity < 0) ||
                pair.Value.Cargo.Where(c => c != null).GroupBy(c => c.ItemId).Any(g => g.Count() > 1))
                throw new InvalidDataException("Invalid economic vessel snapshot.");
        if (state.Rentals.Any(r => r == null)) throw new InvalidDataException("Null rental record.");
        if (state.Rentals.Select(r => r.RentalId).Distinct().Count() != state.Rentals.Count ||
            state.Rentals.Any(r => r == null || !Guid.TryParseExact(r.RentalId, "N", out _) || string.IsNullOrWhiteSpace(r.ShipId) ||
                string.IsNullOrWhiteSpace(r.RenterAccount) || string.IsNullOrWhiteSpace(r.PayerCharacter) ||
                r.PortId != "southport" || !Enum.IsDefined(r.Status) || r.Fee < 0 || r.Deposit < 0 || r.DamageRate < 0 ||
                r.CheckoutHull < 0 || r.ReturnHull < 0 || r.CargoAtReturn < 0 || r.GoldOwed < 0 || r.Guests == null ||
                r.Guests.Any(string.IsNullOrWhiteSpace) || r.Status == ShipRentalStatus.RecoveryRequired && string.IsNullOrWhiteSpace(r.RecoveryReason) || r.DamageSettlement < 0 || r.DepositRefund < 0 ||
                (long)r.Fee + r.Deposit > int.MaxValue || (long)r.DamageSettlement + r.DepositRefund > r.Deposit))
            throw new InvalidDataException("Invalid rental agreement.");
        if (state.Rentals.Where(r => IsOpen(r.Status)).GroupBy(r => r.ShipId).Any(g => g.Count() > 1))
            throw new InvalidDataException("Multiple open rentals share a vessel.");
        if (state.Rentals.Any(r => r.VesselDefinitionId == CutterRentalFleet.DefinitionId && string.IsNullOrWhiteSpace(r.DeckAreaResRef)) ||
            state.Rentals.Where(r => IsOpen(r.Status)).SelectMany(r => new[] { r.DeckAreaResRef, r.CabinAreaResRef }
                .Where(a => !string.IsNullOrWhiteSpace(a))).GroupBy(a => a, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new InvalidDataException("Open rental agreements require isolated decks and cabins.");
        if (state.PendingMoney.Any(i => i == null || string.IsNullOrWhiteSpace(i.Id) || string.IsNullOrWhiteSpace(i.Account) ||
                string.IsNullOrWhiteSpace(i.Character) || string.IsNullOrWhiteSpace(i.ShipId) || !Guid.TryParseExact(i.Id, "N", out _) ||
                string.IsNullOrWhiteSpace(i.Kind) || i.GoldDelta == 0 || i.BeforeGold < 0 || i.AfterGold < 0 ||
                i.BeforeGold > uint.MaxValue || i.AfterGold > uint.MaxValue || i.AfterGold - i.BeforeGold != i.GoldDelta))
            throw new InvalidDataException("Invalid payment journal.");
    }
    public static bool IsOpen(ShipRentalStatus status) => status is ShipRentalStatus.Active or ShipRentalStatus.CheckoutPending or ShipRentalStatus.RecoveryRequired;
    public static (int Buy, int Sell) Prices(MarketCommodity g, int? stock = null)
    {
        long middle = (long)g.BasePrice + (g.TargetStock - (long)(stock ?? g.Stock)) / g.UnitsPerPriceStep;
        int ask = (int)Math.Clamp(middle + (g.Spread + 1L) / 2, (long)g.Floor + g.Spread, g.Ceiling);
        return (ask, ask - g.Spread);
    }
    public static void Adjust(MaritimeMarket market, DateTime now, int seconds)
    {
        if (market.AdjustedAt == default) { market.AdjustedAt = now; return; }
        int periods = (int)Math.Clamp((now - market.AdjustedAt).TotalSeconds / seconds, 0, 12);
        if (periods == 0) return;
        foreach (var g in market.Goods.Values) g.Stock = (int)Math.Clamp(g.Stock + (long)periods * g.PeriodSupply, 0, g.MaximumStock);
        market.AdjustedAt = now;
    }
    public static VesselEconomics Capture(ShipState ship) => new()
    { ShipId = ship.ShipId, Cargo = Copy(ship.Cargo), MerchantGold = ship.MerchantGold, RentalPoolId = ship.RentalPoolId,
        RentalStorageRevision = ship.RentalStorageRevision, RentalStored = ship.IsStored, RentalStoredPort = ship.StoredPortId,
        RentalArea = ship.AreaResRef, RentalX = ship.X, RentalY = ship.Y, RentalZ = ship.Z, RentalHeading = ship.Heading, RentalHull = ship.Hull,
        ArrivalSequence = ship.MerchantArrivalSequence, ArrivalPort = ship.CurrentTradePortId, Departure = ship.PortDepartureTime };
    public static void Apply(ShipState ship, VesselEconomics snapshot, bool restoreRentalStorage = false)
    {
        if (snapshot.Cargo.Sum(c => (long)c.Quantity) > ship.CargoCapacity) throw new InvalidDataException("Saved economic cargo exceeds vessel capacity.");
        if (restoreRentalStorage && snapshot.RentalStorageRevision > ship.RentalStorageRevision)
        {
            if (snapshot.RentalHull > ship.MaximumHull) throw new InvalidDataException("Rental transition hull exceeds definition.");
            ship.IsStored = snapshot.RentalStored; ship.StoredPortId = snapshot.RentalStored ? snapshot.RentalStoredPort : null;
            ship.AreaResRef = snapshot.RentalArea; ship.X = snapshot.RentalX; ship.Y = snapshot.RentalY; ship.Z = snapshot.RentalZ;
            ship.Heading = snapshot.RentalHeading; ship.Hull = snapshot.RentalHull; ship.Stop(); ship.HelmsmanPCKey = null;
        }
        ship.RentalStorageRevision = snapshot.RentalStorageRevision;
        ship.Cargo = Copy(snapshot.Cargo); ship.MerchantGold = snapshot.MerchantGold;
        ship.RentalPoolId = snapshot.RentalPoolId;
        if (snapshot.ArrivalSequence > ship.MerchantArrivalSequence)
        {
            ship.MerchantArrivalSequence = snapshot.ArrivalSequence;
            if (ship.ShipType == ShipType.Merchant && snapshot.ArrivalPort != null)
            { ship.IsInPort = true; ship.CurrentTradePortId = snapshot.ArrivalPort; ship.PortDepartureTime = snapshot.Departure; ship.Underway = false; }
        }
    }
    public static int Cargo(VesselEconomics ship, string item) => ship.Cargo.Where(c => c.ItemId == item).Sum(c => c.Quantity);
    public static void AddCargo(VesselEconomics ship, string item, int amount)
    {
        var cargo = ship.Cargo.FirstOrDefault(c => c.ItemId == item);
        if (cargo == null) ship.Cargo.Add(new() { ItemId = item, Quantity = amount }); else cargo.Quantity += amount;
        ship.Cargo.RemoveAll(c => c.Quantity == 0);
    }
    public static int PortTrade(MaritimeEconomyState state, VesselEconomics ship, int capacity,
        string portId, string item, int quantity, bool buys, long availableGold, DateTime now)
    {
        if (quantity <= 0 || !state.Markets.TryGetValue(portId, out var port) || !port.Goods.TryGetValue(item, out var good))
            throw new InvalidOperationException("Unknown market/good or invalid quantity.");
        if (buys && quantity > good.Stock) throw new InvalidOperationException("Insufficient market stock for this quantity.");
        if (buys && ship.Cargo.Sum(c => (long)c.Quantity) + quantity > capacity)
            throw new InvalidOperationException("Your ship's cargo hold is full for this quantity.");
        if (!buys && (quantity > Cargo(ship, item) || good.Stock + (long)quantity > good.MaximumStock))
            throw new InvalidOperationException("Insufficient cargo or port storage capacity.");
        long total = 0;
        // Marginal sale uses stock AFTER receipt. Buying then selling the same
        // units therefore pays the spread at each matching stock level.
        for (int n = 0; n < quantity; n++)
            total += buys ? Prices(good, good.Stock - n).Buy : Prices(good, good.Stock + n + 1).Sell;
        if (total > int.MaxValue || total <= 0 || (buys && (total > availableGold || port.Treasury + total > port.MaximumTreasury)) ||
            (!buys && (total > port.Treasury || availableGold + total > int.MaxValue)))
            throw new InvalidOperationException("Insufficient gold or treasury/gold limit reached.");
        good.Stock += buys ? -quantity : quantity;
        port.Treasury += buys ? (int)total : -(int)total;
        AddCargo(ship, item, buys ? quantity : -quantity);
        state.RecentTrades.Add(new() { At = now, PortId = portId, ShipId = ship.ShipId, Commodity = item,
            Quantity = quantity, Gold = (int)total, ShipBuys = buys, Counterparty = "port" });
        TrimHistory(state);
        return buys ? -(int)total : (int)total;
    }
    public static void TrimHistory(MaritimeEconomyState state)
    { if (state.RecentTrades.Count > 100) state.RecentTrades.RemoveRange(0, state.RecentTrades.Count - 100); }
}
