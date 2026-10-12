using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Dropdown selection of captured ship identities with fixed, independent action controls.</summary>
[ServiceBinding(typeof(ShipRetrievalNuiService))]
public sealed class ShipRetrievalNuiService : IDisposable
{
    private const float ContentWidth = 680, WindowWidth = 740, WindowChrome = 60;
    private sealed class Window(NuiWindowToken token, DockyardSelectionState selection, NuiGroup actions, NuiGroup footer)
    {
        public NuiWindowToken Token { get; } = token;
        public DockyardSelectionState Selection { get; } = selection;
        public string Port => Selection.Port;
        public NuiGroup Actions { get; } = actions;
        public NuiGroup Footer { get; } = footer;
        public bool Busy { get; set; }
        public bool Publishing { get; set; }
    }
    private readonly ShipRegistryService _registry;
    private readonly ShipStorageService _storage;
    private readonly ShipPortService _ports;
    private readonly ShipRentalService _rentals;
    private readonly ShipAccessService _access;
    private readonly ShipTradeNuiService _trade;
    private readonly ShipPartyTransitionService _partyTravel;
    private readonly IslandService _islands;
    private readonly NuiBind<List<NuiComboEntry>> _modeOptions = new("dockyard_modes"), _shipOptions = new("dockyard_vessels");
    private readonly NuiBind<int> _modeSelection = new("dockyard_mode"), _shipSelection = new("dockyard_vessel");
    private readonly NuiBind<bool> _selectorsEnabled = new("dockyard_selectors_enabled"), _shipsEnabled = new("dockyard_vessels_enabled"),
        _primaryEnabled = new("dockyard_primary_enabled"), _returnEnabled = new("dockyard_return_enabled"),
        _marketEnabled = new("dockyard_market_enabled");
    private readonly NuiBind<string> _summary = new("dockyard_summary"), _primaryReason = new("dockyard_primary_reason"),
        _returnReason = new("dockyard_return_reason"), _marketReason = new("dockyard_market_reason"),
        _feedback = new("dockyard_feedback"), _feedbackFirst = new("dockyard_feedback_first"), _feedbackSecond = new("dockyard_feedback_second");
    private readonly NuiBind<string>[] _details = new[] { "name", "class", "hull", "cargo", "weapons", "location", "availability", "terms", "agreement", "return" }
        .Select(key => new NuiBind<string>("dockyard_detail_" + key)).ToArray();
    private readonly Dictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private NwModule? _module;
    private bool _disposed;

    public ShipRetrievalNuiService(ShipRegistryService registry, ShipStorageService storage, ShipPortService ports,
        ShipRentalService rentals, ShipAccessService access, ShipTradeNuiService trade, IslandService islands, ShipPartyTransitionService partyTravel)
    {
        _registry = registry; _storage = storage; _ports = ports; _rentals = rentals; _access = access;
        _trade = trade; _islands = islands; _partyTravel = partyTravel;
        _trade.ViewChanged += HandleRelatedViewChanged; _partyTravel.TravelFinished += HandleRelatedViewChanged;
    }
    internal void InitializeModule(NwModule module)
    { if (_disposed || _module != null) return; module.OnClientLeave += HandleDisconnect; _module = module; }
    internal async Task RefreshAfterInitializationAsync(Task initialization)
    {
        try
        {
            await initialization; await NwTask.SwitchToMainThread();
            if (_disposed || _module == null) return;
            foreach (var player in _module.Players.Where(p => p.IsValid && _windows.ContainsKey(p.PlayerName)).ToArray())
                HandleRelatedViewChanged(player);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Dockyard refresh after fleet restoration failed."); }
    }
    public async void Open(NwPlayer player, string portId)
    {
        try
        {
            await _rentals.EnrollAsync(); await NwTask.SwitchToMainThread();
            if (_disposed || !player.IsValid) return;
            if (!AtPort(player, portId)) { player.SendServerMessage("Visit the configured shipyard at this port."); return; }
            if (_windows.TryGetValue(player.PlayerName, out var existing) && existing.Port == portId)
            { if (!existing.Busy) Refresh(player, existing); return; }
            Close(player);
            var selection = new DockyardSelectionState(portId, MaritimeIdentity.Account(player));
            CaptureChoices(player, selection);
            var root = CreateLayout(portId, selection);
            if (!player.TryCreateNuiWindow(CreateWindow(root), out var token, "ship_retrieval_window")) return;
            var view = new Window(token, selection, root.Children.OfType<NuiGroup>().Single(g => g.Id == "dockyard_actions"),
                root.Children.OfType<NuiGroup>().Single(g => g.Id == "dockyard_footer"));
            _windows[player.PlayerName] = view;
            token.SetBindValue(_modeOptions, new List<NuiComboEntry>
            { new("RETRIEVE STORED SHIP", (int)DockyardMode.Stored), new("RENT SHIP", (int)DockyardMode.Rent), new("ACTIVE RENTAL", (int)DockyardMode.ActiveRental) });
            player.OnNuiEvent -= HandleEvent; player.OnNuiEvent += HandleEvent;
            Publish(player, view); SetFeedback(view, "Choose a mode and vessel. Actions always use the selected ship.");
        }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Shipyard window initialization failed."); }
    }
    private bool AtPort(NwPlayer player, string port) => player.IsValid && player.LoginCreature == player.ControlledCreature &&
        _islands.GetIslands().Any(i => i.Id == port && i.LandingArea == player.ControlledCreature?.Area?.ResRef);

    private void CaptureChoices(NwPlayer player, DockyardSelectionState state) => state.Refresh(_registry,
        DockyardShipList.Create(_registry, _storage, state.Port, s => _access.CanRetrieve(s, player, false)),
        _rentals.Definitions, _rentals.ForAccount(state.Account));

    private void Refresh(NwPlayer player, Window view)
    {
        if (_disposed || !AtPort(player, view.Port)) { Close(player); return; }
        var selected = view.Selection.Selected;
        CaptureChoices(player, view.Selection);
        Publish(player, view);
        if (selected != null && view.Selection.Selected == null)
            SetFeedback(view, "The selected vessel or agreement is no longer available. Select a vessel explicitly.");
    }
    private void HandleRelatedViewChanged(NwPlayer player)
    {
        if (_disposed || !player.IsValid || !_windows.TryGetValue(player.PlayerName, out var view) || view.Busy) return;
        try { Refresh(player, view); }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Dockyard refresh after travel/trade failed."); }
    }
    private DockyardSelectedVessel Details(NwPlayer player, Window view)
    {
        var state = view.Selection;
        var currentLease = state.Selected?.Agreement == null ? null : _rentals.ForAccount(state.Account).SingleOrDefault(a =>
            a.RentalId == state.Selected!.RentalId && a.ShipId == state.Selected.ShipId && a.PortId == state.Port && a.RenterAccount == state.Account);
        bool current = state.Selected?.Agreement == null || currentLease != null;
        bool offerCurrent = state.Selected?.Offer is not { } captured || _rentals.Definitions.Any(d => d.ShipId == state.Selected.ShipId && d.PortId == state.Port &&
            System.Text.Json.JsonSerializer.Serialize(d) == System.Text.Json.JsonSerializer.Serialize(captured));
        return DockyardSelectedVessel.Capture(state, _registry, _storage, s => _access.CanRetrieve(s, player, false),
            _rentals.Enabled, _rentals.ResourcesAvailable, _ports.HasRentalOccupants, s => _trade.CanOpenPort(player, s), current, offerCurrent, currentLease);
    }
    private void Publish(NwPlayer player, Window view)
    {
        var token = view.Token; var state = view.Selection;
        var details = Details(player, view);
        bool pending = view.Busy || _partyTravel.IsTravelPending(player.PlayerName);
        view.Publishing = true;
        try
        {
            token.SetBindWatch(_modeSelection, false); token.SetBindWatch(_shipSelection, false);
            var entries = new List<NuiComboEntry> { new("SELECT A VESSEL", -1) };
            entries.AddRange(state.Choices.Select(c => new NuiComboEntry(c.Name, c.Value)));
            token.SetBindValue(_shipOptions, entries);
            token.SetBindValue(_modeSelection, (int)state.Mode); token.SetBindValue(_shipSelection, state.SelectedValue);
            token.SetBindValue(_selectorsEnabled, !pending); token.SetBindValue(_shipsEnabled, !pending && state.Choices.Count > 0);
            token.SetBindValue(_summary, state.Choices.Count == 0 ? state.EmptyMessage : $"{state.Choices.Count} vessels in this mode. Select one to view its current details.");
            for (int i = 0; i < _details.Length; i++) token.SetBindValue(_details[i], details.Lines[i]);
            var primary = Primary(state.Mode);
            void Bind(DockyardAction action, NuiBind<bool> enabled, NuiBind<string> reason)
            {
                token.SetBindValue(enabled, !pending && details[action].Visible && details[action].Enabled);
                token.SetBindValue(reason, !details[action].Visible ? "" : pending ? "Finish or cancel the boarding confirmation before another action." : details[action].Reason);
            }
            Bind(primary, _primaryEnabled, _primaryReason); Bind(DockyardAction.ReturnRental, _returnEnabled, _returnReason);
            Bind(DockyardAction.Market, _marketEnabled, _marketReason);
            token.SetGroupLayout(view.Actions, CreateActions(state));
            token.SetGroupLayout(view.Footer, CreateFooter(state));
        }
        finally
        {
            view.Publishing = false;
            token.SetBindWatch(_modeSelection, true); token.SetBindWatch(_shipSelection, true);
        }
    }
    private static DockyardAction Primary(DockyardMode mode) => mode switch
    { DockyardMode.Stored => DockyardAction.Retrieve, DockyardMode.Rent => DockyardAction.Rent, _ => DockyardAction.RetrieveRental };

    private static NuiWindow CreateWindow(NuiColumn root) => new(new NuiGroup
    { Element = root, Width = ContentWidth, Height = root.Height, Border = false, Scrollbars = NuiScrollbars.None, Margin = 0, Padding = 0 },
        "Southport Dockyard") { Geometry = new NuiRect(-1, -1, WindowWidth, root.Height!.Value + WindowChrome), Closable = true, Resizable = false };
    private NuiColumn CreateLayout(string port, DockyardSelectionState state)
    {
        var root = new NuiColumn { Width = ContentWidth, Margin = 0, Padding = 0 };
        root.Children.Add(Readout($"{port.ToUpperInvariant()} DOCKYARD", 32));
        root.Children.Add(Readout("DOCKYARD MODE", 24));
        root.Children.Add(new NuiCombo { Id = "dockyard_mode_selector", Entries = _modeOptions, Selected = _modeSelection,
            Enabled = _selectorsEnabled, Width = ContentWidth, Height = 40, Margin = 0, Padding = 2 });
        root.Children.Add(Readout("SELECT VESSEL", 24));
        root.Children.Add(new NuiCombo { Id = "dockyard_vessel_selector", Entries = _shipOptions, Selected = _shipSelection,
            Enabled = _shipsEnabled, Width = ContentWidth, Height = 40, Margin = 0, Padding = 2 });
        root.Children.Add(Readout(_summary));
        root.Children.Add(Readout("SELECTED SHIP", 28));
        foreach (var bind in _details) root.Children.Add(Readout(bind));
        root.Children.Add(new NuiGroup { Id = "dockyard_actions", Element = CreateActions(state), Width = ContentWidth, Height = 132,
            Border = false, Scrollbars = NuiScrollbars.None, Margin = 0, Padding = 0 });
        root.Children.Add(Readout(_marketReason));
        root.Children.Add(Readout(_feedbackFirst, tooltip: _feedback)); root.Children.Add(Readout(_feedbackSecond, tooltip: _feedback));
        root.Children.Add(new NuiGroup { Id = "dockyard_footer", Element = CreateFooter(state), Width = ContentWidth, Height = 44,
            Border = false, Scrollbars = NuiScrollbars.None, Margin = 0, Padding = 0 });
        root.Children.Add(Readout("Checkout charges fee + deposit once. Disembarking does not end a rental.", 26));
        root.Height = root.Children.Sum(c => c.Height ?? 0); return root;
    }
    private NuiColumn CreateActions(DockyardSelectionState state)
    {
        var primary = Primary(state.Mode);
        string label = state.Mode switch { DockyardMode.Stored => "RETRIEVE SHIP", DockyardMode.Rent => "RENT SHIP", _ => "RETRIEVE RENTAL" };
        var actions = new NuiColumn { Width = ContentWidth, Margin = 0, Padding = 0, Children =
        [Button(label, DockyardSelectedVessel.ActionId(primary, state.SelectedValue), _primaryEnabled, ContentWidth), Readout(_primaryReason)] };
        if (state.Mode == DockyardMode.ActiveRental)
        {
            actions.Children.Add(Button("RETURN RENTAL", DockyardSelectedVessel.ActionId(DockyardAction.ReturnRental, state.SelectedValue), _returnEnabled, ContentWidth));
            actions.Children.Add(Readout(_returnReason));
        }
        actions.Height = actions.Children.Sum(c => c.Height ?? 0); return actions;
    }
    private NuiRow CreateFooter(DockyardSelectionState state)
    {
        var row = new NuiRow { Width = ContentWidth, Height = 44, Margin = 0, Padding = 0, Children =
        [Button("REFRESH", "shipyard_refresh", true, 160), new NuiSpacer { Width = 20, Height = 44, Margin = 0, Padding = 0 }] };
        row.Children.Add(state.Mode == DockyardMode.Rent
            ? new NuiSpacer { Width = 320, Height = 44, Margin = 0, Padding = 0 }
            : Button("CARGO / MARKET", DockyardSelectedVessel.ActionId(DockyardAction.Market, state.SelectedValue), _marketEnabled, 320));
        row.Children.Add(new NuiSpacer { Width = 20, Height = 44, Margin = 0, Padding = 0 });
        row.Children.Add(Button("CLOSE", "ship_retrieval_close", true, 160)); return row;
    }
    private static NuiButton Button(string label, string id, NuiProperty<bool> enabled, float width) => new(label)
    { Id = id, Enabled = enabled, Width = width, Height = 40, Margin = 0, Padding = 2 };
    private static NuiLabel Readout(NuiProperty<string> text, float height = 26, NuiProperty<string>? tooltip = null) => new(text)
    { Width = ContentWidth, Height = height, Margin = 0, Padding = 2, HorizontalAlign = NuiHAlign.Left, VerticalAlign = NuiVAlign.Middle, Tooltip = tooltip ?? text };
    private void SetFeedback(Window view, string message)
    {
        view.Token.SetBindValue(_feedback, message);
        int split = message.Length <= 85 ? message.Length : message.LastIndexOf(' ', 85);
        if (split <= 0) split = Math.Min(85, message.Length);
        view.Token.SetBindValue(_feedbackFirst, message[..split]);
        string remainder = message[split..].TrimStart();
        view.Token.SetBindValue(_feedbackSecond, remainder.Length <= 85 ? remainder : remainder[..82] + "...");
    }
    private async void HandleEvent(ModuleEvents.OnNuiEvent evt)
    {
        var player = evt.Player;
        if (!_windows.TryGetValue(player.PlayerName, out var view) || view.Token.Token != evt.Token.Token || view.Selection.Closed) return;
        if (evt.EventType == NuiEventType.Close || evt.EventType == NuiEventType.Click && evt.ElementId == "ship_retrieval_close") { Close(player); return; }
        if (view.Busy || view.Publishing) return;
        try
        {
            if (!AtPort(player, view.Port)) { Close(player); return; }
            if (evt.EventType == NuiEventType.Watch)
            {
                if (_partyTravel.IsTravelPending(player.PlayerName)) { Publish(player, view); return; }
                if (evt.ElementId == _modeSelection.Key)
                {
                    int value = view.Token.GetBindValue(_modeSelection);
                    if (value == (int)view.Selection.Mode) return;
                    view.Selection.ChangeMode(value); Publish(player, view); return;
                }
                if (evt.ElementId == _shipSelection.Key)
                {
                    int value = view.Token.GetBindValue(_shipSelection);
                    if (value == view.Selection.SelectedValue) return;
                    if (!view.Selection.Select(value)) SetFeedback(view, "That dropdown entry is stale. Select a current vessel.");
                    Publish(player, view); return;
                }
                return;
            }
            if (evt.EventType != NuiEventType.Click) return;
            if (evt.ElementId == "shipyard_refresh") { Refresh(player, view); return; }
            // No ArrayIndex is involved. The clicked action encodes the captured selection value.
            if (_partyTravel.IsTravelPending(player.PlayerName) || view.Token.GetBindValue(_modeSelection) != (int)view.Selection.Mode ||
                view.Token.GetBindValue(_shipSelection) != view.Selection.SelectedValue ||
                !DockyardSelectedVessel.TryAction(evt.ElementId, view.Selection, out var action))
            { SetFeedback(view, "The selection changed or confirmation is pending. Review the selected vessel before acting."); return; }
            var details = Details(player, view);
            if (!details[action].Visible || !details[action].Enabled || !view.Selection.TryResolve(_registry, out var ship))
            { Publish(player, view); SetFeedback(view, details[action].Reason); return; }
            view.Busy = true;
            try
            {
                Publish(player, view);
                if (action == DockyardAction.Market) _trade.OpenPort(player, ship!);
                else if (action == DockyardAction.Rent) _partyTravel.OpenCheckout(player, ship!, view.Port);
                else if (action is DockyardAction.Retrieve or DockyardAction.RetrieveRental) _partyTravel.OpenRetrieval(player, ship!, view.Port);
                else
                {
                    var lease = view.Selection.Selected!.Agreement!;
                    // Details rechecked the account, canonical ship, exact agreement, storage and occupants.
                    var result = await _rentals.ReturnAsync(player, lease.ShipId, view.Port);
                    await NwTask.SwitchToMainThread();
                    if (player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == view)
                    { player.SendServerMessage(result.Message); SetFeedback(view, result.Message); }
                }
            }
            finally
            {
                await NwTask.SwitchToMainThread();
                view.Busy = false;
                if (!_disposed && player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == view) Refresh(player, view);
            }
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(ex, "Dropdown dockyard interaction failed.");
            await NwTask.SwitchToMainThread();
            if (player.IsValid && _windows.GetValueOrDefault(player.PlayerName) == view)
                SetFeedback(view, "The action could not complete. REFRESH to review the current vessel state.");
        }
    }
    public void Close(NwPlayer player)
    {
        player.OnNuiEvent -= HandleEvent;
        if (!_windows.Remove(player.PlayerName, out var view)) return;
        view.Selection.Close(); SailingWindowLifecycle.Close(view.Token);
    }
    private void HandleDisconnect(ModuleEvents.OnClientLeave evt) => Close(evt.Player);
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _trade.ViewChanged -= HandleRelatedViewChanged; _partyTravel.TravelFinished -= HandleRelatedViewChanged;
        if (_module != null) _module.OnClientLeave -= HandleDisconnect; _module = null;
        foreach (var view in _windows.Values.ToArray()) Close(view.Token.Player);
    }
}
