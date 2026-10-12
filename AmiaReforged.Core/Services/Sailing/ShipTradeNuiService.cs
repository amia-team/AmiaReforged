using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Core.Services.Sailing.SailingNuiLayout;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Captured merchant/port identity and stable table rows for the lifetime of each window.</summary>
[ServiceBinding(typeof(ShipTradeNuiService))]
public sealed class ShipTradeNuiService : IDisposable
{
    // Presentation observers refresh cargo information; they do not participate in transactions.
    public event Action<NwPlayer>? ViewChanged;
    private sealed class View(ShipTradeViewState state, NuiWindowToken token)
    {
        public ShipTradeViewState State { get; } = state;
        public NuiWindowToken Token { get; } = token;
        public bool Busy { get; set; }
    }
    private readonly Dictionary<string, View> _windows = new();
    private readonly MerchantTradeService _trade;
    private readonly PhysicalShipService _physical;
    private readonly ShipStatePersistenceService _persistence;
    private readonly IslandService _islands;
    private readonly NuiBind<string> _status = new("maritime_status"), _cargo = new("maritime_cargo"),
        _counterparty = new("maritime_counterparty"), _feedback = new("maritime_feedback"),
        _buyName = new("maritime_buy_name"), _buyStock = new("maritime_buy_stock"), _buyPrice = new("maritime_buy_price"),
        _marketSellPrice = new("maritime_market_sell_price"), _buyQuantity = new("maritime_buy_quantity"),
        _sellName = new("maritime_sell_name"), _aboard = new("maritime_aboard"), _sellPrice = new("maritime_sell_price"),
        _receivingSpace = new("maritime_receiving_space"), _sellQuantity = new("maritime_sell_quantity"), _market = new("maritime_market");
    private readonly NuiBind<int> _buyCount = new("maritime_buy_count"), _sellCount = new("maritime_sell_count");
    private readonly NuiBind<bool> _buyEnabled = new("maritime_buy_enabled"), _sellEnabled = new("maritime_sell_enabled");
    private NwModule? _module;
    private bool _disposed;
    public ShipTradeNuiService(MerchantTradeService trade, PhysicalShipService physical, ShipStatePersistenceService persistence, IslandService islands)
    { _trade = trade; _physical = physical; _persistence = persistence; _islands = islands; physical.PlayerDetached += HandleDetached; }
    internal void InitializeModule(NwModule module)
    { if (_disposed || _module != null) return; _module = module; module.OnClientLeave += HandleDisconnect; }
    public static bool IsEligible(ShipTradeSession session) => ShipActionRules.CanTrade(session.Source, session.Merchant, session.PortId);
    public void Open(NwPlayer player, ShipState source, ShipState? merchant)
    {
        if (_disposed) return;
        if (merchant?.CurrentTradePortId is not { } port || !_trade.WithMerchant(source, merchant, port, player))
        { player.SendServerMessage("Select an available merchant trading at a port and remain within trading range."); return; }
        Create(player, source, merchant, port);
    }
    public void OpenPort(NwPlayer player, ShipState source)
    {
        if (_disposed) return;
        string? island = source.IsStored ? source.StoredPortId : _islands.GetNearestIsland(source)?.Id;
        string? port = island == null ? null : _trade.MarketForIsland(island);
        if (port == null || !_trade.AtMarket(source, port, player))
        { player.SendServerMessage("This ship must be at a configured port, with its renter or captain present."); return; }
        Create(player, source, null, port);
    }
    public bool CanOpenPort(NwPlayer player, ShipState source)
    {
        string? island = source.IsStored ? source.StoredPortId : _islands.GetNearestIsland(source)?.Id;
        string? port = island == null ? null : _trade.MarketForIsland(island);
        return !_disposed && port != null && _trade.AtMarket(source, port, player);
    }
    private NuiColumn CreateLayout(ShipTradeViewState state)
    {
        NuiColumn column = new() { Width = 950, Margin = 0, Padding = 0 };
        column.Children.Add(Text($"{(state.Merchant == null ? "PORT MARKET" : "TRADE WITH " + state.Merchant.ShipName)}\nMarket: {state.Port} / Your ship: {state.Source.ShipName}", 56));
        column.Children.Add(Text(_status, 44)); column.Children.Add(Text(_counterparty, 48));
        column.Children.Add(Label("AVAILABLE GOODS / BUY"));
        column.Children.Add(Headers("Commodity", "Available", "Buy gp/unit", "Sell gp/unit", "Quantity", "Purchase"));
        column.Children.Add(new NuiList([
            Cell(Text(_buyName, 48), 300), Cell(Text(_buyStock, 48), 120), Cell(Label(_buyPrice, 40), 120),
            Cell(Label(_marketSellPrice, 40), 120),
            Cell(new NuiTextEdit("Buy qty", _buyQuantity, 5, false) { Height = 36, Id = "maritime_buy_qty" }, 120),
            Cell(new NuiButton("BUY") { Id = "maritime_buy", Height = 36, Enabled = _buyEnabled }, 140)
        ], _buyCount) { Width = 940, Height = 170, RowHeight = 60 });
        column.Children.Add(Label("GOODS ON BOARD / SELL"));
        column.Children.Add(Headers("Commodity", "Aboard", "Sell gp/unit", "Buyer room", "Quantity", "Sale"));
        column.Children.Add(new NuiList([
            Cell(Text(_sellName, 48), 300), Cell(Label(_aboard, 40), 120), Cell(Label(_sellPrice, 40), 120),
            Cell(Label(_receivingSpace, 40), 120),
            Cell(new NuiTextEdit("Sell qty", _sellQuantity, 5, false) { Height = 36, Id = "maritime_sell_qty" }, 120),
            Cell(new NuiButton("SELL") { Id = "maritime_sell", Height = 36, Enabled = _sellEnabled }, 140)
        ], _sellCount) { Width = 940, Height = 170, RowHeight = 60 });
        column.Children.Add(Label(_cargo)); column.Children.Add(Text(_market, 44));
        column.Children.Add(Text(_feedback, 68));
        column.Children.Add(new NuiRow { Height = 40, Children = [new NuiButton("REFRESH") { Id = "maritime_refresh", Height = 36 },
            new NuiButton("CLOSE") { Id = "maritime_close", Height = 36 }] });
        return column;
    }
    private static NuiRow Headers(params string[] headings)
    {
        float[] widths = [300, 120, 120, 120, 120, 140];
        return new NuiRow { Height = 32, Margin = 0, Padding = 0,
            Children = headings.Select((heading, i) => (NuiElement)new NuiLabel(heading)
            { Width = widths[i], Height = 28, Margin = 0, Padding = 2, HorizontalAlign = NuiHAlign.Left }).ToList() };
    }
    private void Create(NwPlayer player, ShipState source, ShipState? merchant, string port)
    {
        if (_windows.TryGetValue(player.PlayerName, out var existing) && existing.Busy)
        { player.SendServerMessage("Wait for your current trade to finish before opening another market."); return; }
        Close(player);
        var state = new ShipTradeViewState(source, merchant, port);
        state.Refresh(_trade.State);
        var window = SailingNuiLayout.Window(CreateLayout(state), "Maritime Trade", 1010, 870);
        if (!player.TryCreateNuiWindow(window, out var token, "merchant_trade")) return;
        _windows[player.PlayerName] = new(state, token);
        player.OnNuiEvent -= HandleEvent; player.OnNuiEvent += HandleEvent;
        token.SetBindValue(_feedback, $"Enter a quantity of 1-{_trade.MaximumQuantity} on the commodity's row. Prices show the first unit; port totals follow marginal stock prices at transaction time.");
        Refresh(player, captureQuantities: false);
    }
    private bool Valid(NwPlayer player, View window) => window.State.Merchant == null
        ? _trade.AtMarket(window.State.Source, window.State.Port, player) :
        _trade.WithMerchant(window.State.Source, window.State.Merchant, window.State.Port, player);
    private void CaptureQuantities(View view)
    {
        view.State.CaptureQuantities(true, view.Token.GetBindValues(_buyQuantity) ?? []);
        view.State.CaptureQuantities(false, view.Token.GetBindValues(_sellQuantity) ?? []);
    }
    private async void HandleEvent(ModuleEvents.OnNuiEvent evt)
    {
        var player = evt.Player; string element = evt.ElementId;
        if (!_windows.TryGetValue(player.PlayerName, out var window) || window.Token.Token != evt.Token.Token) return;
        if (evt.EventType == NuiEventType.Close || evt.EventType == NuiEventType.Click && element == "maritime_close") { Close(player); return; }
        if (evt.EventType != NuiEventType.Click || window.Busy) return;
        if (element == "maritime_refresh") { Refresh(player); return; }
        if (element is not ("maritime_buy" or "maritime_sell")) return;
        bool buys = element == "maritime_buy";
        CaptureQuantities(window);
        if (!Valid(player, window))
        { window.Token.SetBindValue(_feedback, "The captured merchant or market is unavailable or out of range. Return to it, or close and explicitly select a new trading partner."); Refresh(player); return; }
        if (!window.State.TryResolve(buys, evt.ArrayIndex, _trade.MaximumQuantity, out string item, out int quantity))
        { window.Token.SetBindValue(_feedback, $"Invalid row or quantity. Enter a whole number from 1 to {_trade.MaximumQuantity} in this row."); return; }
        window.Busy = true; Refresh(player);
        try
        {
            var result = await _trade.PlayerTradeAsync(player, window.State.Source, window.State.Merchant, window.State.Port, item, quantity, buys);
            await NwTask.SwitchToMainThread();
            if (result.Success)
            {
                await _persistence.TrySaveState(window.State.Source);
                if (window.State.Merchant != null) await _persistence.TrySaveState(window.State.Merchant);
                await NwTask.SwitchToMainThread();
            }
            if (player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == window)
                window.Token.SetBindValue(_feedback, result.Message);
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(ex, "Maritime trade window operation failed.");
            await NwTask.SwitchToMainThread();
            if (player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == window)
                window.Token.SetBindValue(_feedback, "The transaction could not complete. Refresh to see the saved cargo and payment state.");
        }
        finally
        {
            window.Busy = false;
            if (player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == window) Refresh(player);
        }
    }
    private void Refresh(NwPlayer player, bool captureQuantities = true)
    {
        if (!_windows.TryGetValue(player.PlayerName, out var window) || !player.IsValid) return;
        if (captureQuantities) CaptureQuantities(window);
        var state = window.State; var token = window.Token; var snapshot = _trade.State;
        state.Refresh(snapshot); bool valid = Valid(player, window);
        static string Price(int? price) => price is { } p ? p + " gp" : "Unavailable";
        token.SetBindValue(_buyCount, 0); token.SetBindValue(_sellCount, 0);
        token.SetBindValues(_buyName, state.Buying.Select(r => r.Name));
        token.SetBindValues(_buyStock, state.Buying.Select(r => state.Merchant == null ? r.Stock.ToString() : $"{r.Stock} aboard\n{r.MarketStock} port"));
        token.SetBindValues(_buyPrice, state.Buying.Select(r => Price(r.BuyPrice)));
        token.SetBindValues(_marketSellPrice, state.Buying.Select(r => Price(r.SellPrice)));
        token.SetBindValues(_buyQuantity, state.Quantities(true));
        token.SetBindValues(_buyEnabled, state.Buying.Select(r => valid && !window.Busy && r.BuyPrice != null));
        token.SetBindValues(_sellName, state.Selling.Select(r => r.Name)); token.SetBindValues(_aboard, state.Selling.Select(r => r.Aboard.ToString()));
        token.SetBindValues(_sellPrice, state.Selling.Select(r => Price(r.SellPrice)));
        token.SetBindValues(_receivingSpace, state.Selling.Select(r => r.ReceivingSpace.ToString()));
        token.SetBindValues(_sellQuantity, state.Quantities(false));
        token.SetBindValues(_sellEnabled, state.Selling.Select(r => valid && !window.Busy && r.SellPrice != null && r.Aboard > 0));
        token.SetBindValue(_buyCount, state.Buying.Count); token.SetBindValue(_sellCount, state.Selling.Count);
        token.SetBindValue(_cargo, $"Cargo used: {state.Used} / {state.Source.CargoCapacity} | Remaining: {state.Remaining} | Your gold: {player.LoginCreature?.Gold ?? 0} gp");
        token.SetBindValue(_status, valid ? window.Busy ? "Transaction in progress..." : "Trade eligible / within range. This trading partner remains selected until you close the window." :
            "Trade unavailable / out of range or stale ship. Refresh after returning; choose another partner explicitly after closing.");
        token.SetBindValue(_counterparty, state.Merchant is { } m
            ? $"Selected merchant: {m.ShipName} | Gold: {m.MerchantGold} gp\nMerchant cargo: {m.Cargo.Sum(c => (long)c.Quantity)} / {m.CargoCapacity}"
            : $"Selected port: {state.Port}. Cargo remains aboard {state.Source.ShipName}.");
        token.SetBindValue(_market, snapshot.Markets.TryGetValue(state.Port, out var market)
            ? $"Market: {state.Port} | Treasury: {market.Treasury} gp | {state.Buying.Count} commodities\nGoods on board: {state.Selling.Count(r => r.Aboard > 0)} types."
            : "Market unavailable. No transactions are permitted.");
        NotifyViewChanged(player);
    }
    private void NotifyViewChanged(NwPlayer player)
    {
        if (ViewChanged == null) return;
        foreach (Action<NwPlayer> observer in ViewChanged.GetInvocationList())
            try { observer(player); }
            catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Trade presentation observer failed."); }
    }
    public void Close(NwPlayer player)
    { player.OnNuiEvent -= HandleEvent; if (_windows.Remove(player.PlayerName, out var w)) { w.State.Close(); SailingWindowLifecycle.Close(w.Token); NotifyViewChanged(player); } }
    private void HandleDetached(string ship, string name)
    { if (_windows.TryGetValue(name, out var window) && window.State.Source.ShipName == ship) Close(window.Token.Player); }
    private void HandleDisconnect(ModuleEvents.OnClientLeave evt) => Close(evt.Player);
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _physical.PlayerDetached -= HandleDetached;
        if (_module != null) _module.OnClientLeave -= HandleDisconnect; _module = null;
        foreach (var window in _windows.Values.ToArray()) Close(window.Token.Player);
    }
}
