using System.Globalization;
using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public sealed record ShipTradeRow(string Item, string Name, int Stock, int MarketStock, int Aboard,
    int? BuyPrice, int? SellPrice, int ReceivingSpace);

/// <summary>Rows are append-only for a window token, so a delayed index never addresses another commodity.</summary>
public sealed class ShipTradeViewState(ShipState source, ShipState? merchant, string port)
{
    public ShipState Source { get; } = source;
    public ShipState? Merchant { get; } = merchant;
    public string Port { get; } = port;
    public bool Closed { get; private set; }
    private readonly List<string> _buy = [], _sell = [];
    private readonly Dictionary<string, string> _buyQuantities = new(StringComparer.Ordinal), _sellQuantities = new(StringComparer.Ordinal);
    public IReadOnlyList<ShipTradeRow> Buying { get; private set; } = [];
    public IReadOnlyList<ShipTradeRow> Selling { get; private set; } = [];
    public long Used => Source.Cargo.Sum(c => (long)c.Quantity);
    public long Remaining => Math.Max(0, Source.CargoCapacity - Used);
    public bool IsCurrent(ShipRegistryService registry) => !Closed && registry.IsCurrent(Source) && (Merchant == null || registry.IsCurrent(Merchant));
    public void Refresh(MaritimeEconomyState state)
    {
        if (Closed) return;
        state.Markets.TryGetValue(Port, out var market);
        foreach (string item in (market?.Goods.Keys.AsEnumerable() ?? []).OrderBy(s => s, StringComparer.Ordinal))
            if (!_buy.Contains(item)) { _buy.Add(item); _buyQuantities[item] = "1"; }
        foreach (string item in Source.Cargo.Where(c => c.Quantity > 0).Select(c => c.ItemId).Distinct().OrderBy(s => s, StringComparer.Ordinal))
            if (!_sell.Contains(item)) { _sell.Add(item); _sellQuantities[item] = "1"; }
        ShipTradeRow Row(string item)
        {
            MarketCommodity? good = market?.Goods.GetValueOrDefault(item);
            int stock = Merchant == null ? good?.Stock ?? 0 : Merchant.Cargo.Where(c => c.ItemId == item).Sum(c => c.Quantity);
            int aboard = Source.Cargo.Where(c => c.ItemId == item).Sum(c => c.Quantity);
            int? buy = good == null ? null : MaritimeMarketEngine.Prices(good).Buy;
            // Port sales use the stock after the first receipt; merchant exchange uses the current quote.
            int? sell = good == null ? null : MaritimeMarketEngine.Prices(good, Merchant == null ? Math.Min(good.MaximumStock, good.Stock + 1) : good.Stock).Sell;
            int room = Merchant == null ? good == null ? 0 : good.MaximumStock - good.Stock :
                (int)Math.Max(0, Merchant.CargoCapacity - Merchant.Cargo.Sum(c => (long)c.Quantity));
            return new(item, CultureInfo.InvariantCulture.TextInfo.ToTitleCase(item.Replace('_', ' ')), stock, good?.Stock ?? 0, aboard, buy, sell, room);
        }
        Buying = Array.AsReadOnly(_buy.Select(Row).ToArray()); Selling = Array.AsReadOnly(_sell.Select(Row).ToArray());
    }
    public string[] Quantities(bool buys) => (buys ? _buy : _sell).Select(item => (buys ? _buyQuantities : _sellQuantities)[item]).ToArray();
    public void CaptureQuantities(bool buys, IReadOnlyList<string> quantities)
    {
        if (Closed) return;
        var items = buys ? _buy : _sell; var values = buys ? _buyQuantities : _sellQuantities;
        for (int i = 0; i < Math.Min(items.Count, quantities.Count); i++) values[items[i]] = quantities[i];
    }
    public bool TryResolve(bool buys, int index, int maximum, out string item, out int quantity)
    {
        item = ""; quantity = 0; var items = buys ? _buy : _sell;
        if (Closed || index < 0 || index >= items.Count) return false;
        item = items[index]; string value = (buys ? _buyQuantities : _sellQuantities)[item];
        return int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out quantity) && quantity >= 1 && quantity <= maximum;
    }
    public void Close() => Closed = true;
}
