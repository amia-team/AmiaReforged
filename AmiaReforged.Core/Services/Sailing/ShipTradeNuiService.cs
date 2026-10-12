using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Captures the selected merchant or explicit port and vessel; never falls back to encounter ordering.</summary>
[ServiceBinding(typeof(ShipTradeNuiService))]
public sealed class ShipTradeNuiService : IDisposable
{
    private sealed class Window(ShipState source, ShipState? merchant, string port, NuiWindowToken token, MerchantTradeOffer[] offers)
    {
        public ShipState Source { get; } = source;
        public ShipState? Merchant { get; } = merchant;
        public string Port { get; } = port;
        public NuiWindowToken Token { get; } = token;
        public MerchantTradeOffer[] Offers { get; } = offers;
        public bool Busy { get; set; }
    }
    private readonly Dictionary<string, Window> _windows = new();
    private readonly MerchantTradeService _trade;
    private readonly PhysicalShipService _physical;
    private readonly ShipStatePersistenceService _persistence;
    private readonly IslandService _islands;
    private readonly NuiBind<string> _status = new("maritime_status"), _market = new("maritime_market"), _cargo = new("maritime_cargo"),
        _counterparty = new("maritime_counterparty"), _feedback = new("maritime_feedback"), _activity = new("maritime_activity"), _quantity = new("maritime_quantity");
    private NwModule? _module;
    private bool _disposed;
    public ShipTradeNuiService(MerchantTradeService trade, PhysicalShipService physical, ShipStatePersistenceService persistence, IslandService islands)
    { _trade = trade; _physical = physical; _persistence = persistence; _islands = islands; physical.PlayerDetached += HandleDetached; }
    internal void InitializeModule(NwModule module)
    { if (_disposed || _module != null) return; _module = module; module.OnClientLeave += HandleDisconnect; }
    public static bool IsEligible(ShipTradeSession session) => ShipActionRules.CanTrade(session.Source, session.Merchant, session.PortId);
    public void Open(NwPlayer player, ShipState source, ShipState? merchant)
    {
        if (merchant?.CurrentTradePortId is not { } port || !_trade.WithMerchant(source, merchant, port, player))
        { player.SendServerMessage("Select an available merchant trading at a port."); return; }
        Create(player, source, merchant, port);
    }
    public void OpenPort(NwPlayer player, ShipState source)
    {
        string? island = source.IsStored ? source.StoredPortId : _islands.GetNearestIsland(source)?.Id;
        string? port = island == null ? null : _trade.MarketForIsland(island);
        if (port == null || !_trade.AtMarket(source, port, player))
        { player.SendServerMessage("This ship must be at a configured port, with its renter or captain present."); return; }
        Create(player, source, null, port);
    }
    private void Create(NwPlayer player, ShipState source, ShipState? merchant, string port)
    {
        Close(player);
        var offers = _trade.GetPlayerOffers(port).ToArray();
        NuiColumn column = new();
        column.Children.Add(new NuiLabel($"{(merchant == null ? port + " PORT MARKET" : "TRADE WITH " + merchant.ShipName)} / {source.ShipName}"));
        column.Children.Add(new NuiLabel(_status)); column.Children.Add(new NuiLabel(_cargo));
        column.Children.Add(new NuiLabel(_counterparty)); column.Children.Add(new NuiLabel(_market) { Height = 65 });
        column.Children.Add(new NuiLabel(_activity) { Height = 50 });
        column.Children.Add(new NuiTextEdit("Quantity", _quantity, 5, false));
        for (int i = 0; i < offers.Length; i++)
            column.Children.Add(new NuiButton($"{(offers[i].PlayerBuys ? "BUY" : "SELL")} {offers[i].ItemId.ToUpperInvariant()}") { Id = $"maritime_trade_{i}" });
        column.Children.Add(new NuiLabel(_feedback) { Height = 60 });
        column.Children.Add(new NuiButton("REFRESH") { Id = "maritime_refresh" });
        column.Children.Add(new NuiButton("CLOSE") { Id = "maritime_close" });
        var window = new NuiWindow(column, "Maritime Trade") { Geometry = new NuiRect(-1, -1, 670, 640), Closable = true };
        if (!player.TryCreateNuiWindow(window, out var token, "merchant_trade")) return;
        _windows[player.PlayerName] = new(source, merchant, port, token, offers);
        player.OnNuiEvent -= HandleEvent; player.OnNuiEvent += HandleEvent;
        token.SetBindValue(_quantity, "1"); token.SetBindValue(_feedback, "Prices use actual stock at transaction time. Quantity quotes include marginal price changes.");
        Refresh(player);
    }
    private bool Valid(NwPlayer player, Window window) => window.Merchant == null
        ? _trade.AtMarket(window.Source, window.Port, player) : _trade.WithMerchant(window.Source, window.Merchant, window.Port, player);
    private async void HandleEvent(ModuleEvents.OnNuiEvent evt)
    {
        var player = evt.Player; string element = evt.ElementId;
        if (!_windows.TryGetValue(player.PlayerName, out var window) || window.Token.Token != evt.Token.Token) return;
        if (evt.EventType == NuiEventType.Close || evt.EventType == NuiEventType.Click && element == "maritime_close") { Close(player); return; }
        if (evt.EventType != NuiEventType.Click || window.Busy) return;
        if (element == "maritime_refresh") { Refresh(player); return; }
        const string prefix = "maritime_trade_";
        if (!element.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(element[prefix.Length..], out int row) || row < 0 || row >= window.Offers.Length) return;
        if (!Valid(player, window)) { Close(player); player.SendServerMessage("This captured trade interaction has ended."); return; }
        if (!int.TryParse(window.Token.GetBindValue(_quantity), out int quantity) || quantity < 1)
        { window.Token.SetBindValue(_feedback, "Enter a positive whole quantity."); return; }
        window.Busy = true;
        try
        {
            var offer = window.Offers[row];
            var result = await _trade.PlayerTradeAsync(player, window.Source, window.Merchant, window.Port, offer.ItemId, quantity, offer.PlayerBuys);
            await NwTask.SwitchToMainThread();
            if (result.Success)
            {
                await _persistence.TrySaveState(window.Source);
                if (window.Merchant != null) await _persistence.TrySaveState(window.Merchant);
                await NwTask.SwitchToMainThread();
            }
            if (player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == window)
            { window.Token.SetBindValue(_feedback, result.Message); Refresh(player); }
        }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Maritime trade window operation failed."); }
        finally { window.Busy = false; }
    }
    private void Refresh(NwPlayer player)
    {
        if (!_windows.TryGetValue(player.PlayerName, out var window) || !player.IsValid) return;
        static string Cargo(ShipState s) => $"{s.Cargo.Sum(c => c.Quantity)}/{s.CargoCapacity}: " + string.Join(", ", s.Cargo.Select(c => $"{c.ItemId}={c.Quantity}"));
        window.Token.SetBindValue(_status, $"Gold: {player.LoginCreature?.Gold} | Remaining capacity: {window.Source.CargoCapacity - window.Source.Cargo.Sum(c => c.Quantity)}");
        window.Token.SetBindValue(_cargo, "Your ship cargo: " + Cargo(window.Source));
        window.Token.SetBindValue(_counterparty, window.Merchant is { } m ? $"Merchant gold: {m.MerchantGold}; stock/capacity: {Cargo(m)}" : "Port cargo stays aboard your stored or active vessel.");
        window.Token.SetBindValue(_market, _trade.DescribeMarket(window.Port));
        window.Token.SetBindValue(_activity, "Recent activity: " + string.Join(" | ", _trade.State.RecentTrades
            .Where(t => t.PortId == window.Port).TakeLast(3).Select(t => $"{t.ShipId} {(t.ShipBuys ? "bought" : "sold")} {t.Quantity} {t.Commodity} ({t.Gold} gp)")));
    }
    public void Close(NwPlayer player)
    { player.OnNuiEvent -= HandleEvent; if (_windows.Remove(player.PlayerName, out var w)) SailingWindowLifecycle.Close(w.Token); }
    private void HandleDetached(string ship, string name)
    {
        if (!_windows.TryGetValue(name, out var window) || window.Source.ShipName != ship) return;
        var player = window.Token.Player; Close(player);
    }
    private void HandleDisconnect(ModuleEvents.OnClientLeave evt) => Close(evt.Player);
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _physical.PlayerDetached -= HandleDetached;
        if (_module != null) _module.OnClientLeave -= HandleDisconnect; _module = null;
        foreach (var window in _windows.Values.ToArray()) Close(window.Token.Player);
    }
}
