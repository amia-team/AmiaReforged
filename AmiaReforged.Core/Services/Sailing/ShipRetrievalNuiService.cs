using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

/// <summary>The existing shipyard supports communal retrieval, rental agreements and stored cargo trading.</summary>
[ServiceBinding(typeof(ShipRetrievalNuiService))]
public sealed class ShipRetrievalNuiService : IDisposable
{
    private sealed class Window(NuiWindowToken token, string port, DockyardShipList ships,
        RentalVesselOption[] rentals, ShipRentalRecord[] leases)
    {
        public NuiWindowToken Token { get; } = token;
        public string Port { get; } = port;
        public DockyardShipList Ships { get; } = ships;
        public RentalVesselOption[] Rentals { get; } = rentals;
        public ShipRentalRecord[] Leases { get; } = leases;
        public bool Busy { get; set; }
    }
    private readonly ShipRegistryService _registry;
    private readonly ShipStorageService _storage;
    private readonly ShipPortService _ports;
    private readonly ShipRentalService _rentals;
    private readonly ShipAccessService _access;
    private readonly ShipTradeNuiService _trade;
    private readonly IslandService _islands;
    private readonly NuiBind<int> _storedCount = new("dockyard_stored_count"), _rentalCount = new("dockyard_rental_count"),
        _leaseCount = new("dockyard_lease_count");
    private readonly NuiBind<string> _storedNames = new("dockyard_stored_names"), _storedVessels = new("dockyard_stored_vessels"),
        _storedStatuses = new("dockyard_stored_statuses"), _storedSummary = new("dockyard_stored_summary"),
        _rentalNames = new("dockyard_rental_names"), _rentalSpecs = new("dockyard_rental_specs"),
        _rentalTerms = new("dockyard_rental_terms"), _rentalSummary = new("dockyard_rental_summary"),
        _leaseNames = new("dockyard_lease_names"), _leaseTerms = new("dockyard_lease_terms"),
        _leaseSummary = new("dockyard_lease_summary");
    private readonly NuiBind<bool> _retrieveEnabled = new("dockyard_retrieve_enabled"), _marketEnabled = new("dockyard_market_enabled"),
        _rentEnabled = new("dockyard_rent_enabled"), _returnEnabled = new("dockyard_return_enabled");
    private NwModule? _module;
    private bool _disposed;
    private readonly Dictionary<string, Window> _windows = new();
    public ShipRetrievalNuiService(ShipRegistryService registry, ShipStorageService storage, ShipPortService ports,
        ShipRentalService rentals, ShipAccessService access, ShipTradeNuiService trade, IslandService islands)
    { _registry = registry; _storage = storage; _ports = ports; _rentals = rentals; _access = access; _trade = trade; _islands = islands; }
    internal void InitializeModule(NwModule module)
    { if (_disposed || _module != null) return; module.OnClientLeave += HandleDisconnect; _module = module; }
    internal async Task RefreshAfterInitializationAsync(Task initialization)
    {
        try
        {
            await initialization;
            await NwTask.SwitchToMainThread();
            if (_disposed || _module == null) return;
            foreach (var player in _module.Players.Where(p => p.IsValid && _windows.ContainsKey(p.PlayerName)).ToArray())
            {
                if (!_windows.TryGetValue(player.PlayerName, out var view) || view.Busy) continue;
                try { await OpenAsync(player, view.Port); }
                catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Dockyard refresh after fleet restoration failed."); }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Dockyard awaits failed fleet restoration; rental checkout remains disabled."); }
        catch (OperationCanceledException) { }
    }
    public async void Open(NwPlayer player, string portId)
    {
        try { await OpenAsync(player, portId); }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Shipyard window initialization failed."); }
    }
    private async Task OpenAsync(NwPlayer player, string portId)
    {
        await _rentals.EnrollAsync();
        await NwTask.SwitchToMainThread();
        if (_disposed || !player.IsValid) return;
        if (!_islands.GetIslands().Any(i => i.Id == portId && i.LandingArea == player.ControlledCreature?.Area?.ResRef))
        { player.SendServerMessage("Visit the configured shipyard at this port."); return; }
        // Refresh creates a new token and snapshot. Old row events never address a newly reordered list.
        Close(player);
        var ships = DockyardShipList.Create(_registry, _storage, portId, s => _access.CanRetrieve(s, player, false));
        var definitions = _rentals.Definitions.Where(d => d.PortId == portId).Select(MaritimeMarketEngine.Copy).ToArray();
        var leases = _rentals.ForAccount(MaritimeIdentity.Account(player)).Where(r => r.PortId == portId)
            .OrderBy(r => r.ShipId, StringComparer.Ordinal).ThenBy(r => r.RentalId, StringComparer.Ordinal).ToArray();
        NuiWindow window = new(CreateLayout(portId), "Southport Docks / Shipyard")
        { Geometry = new NuiRect(-1, -1, 980, 800), Closable = true };
        if (!player.TryCreateNuiWindow(window, out var token, "ship_retrieval_window")) return;
        var view = new Window(token, portId, ships, definitions, leases);
        _windows[player.PlayerName] = view;
        PopulateBinds(view);
        player.OnNuiEvent -= HandleEvent; player.OnNuiEvent += HandleEvent;
    }

    private static NuiLabel Label(NuiProperty<string> text) => new(text) { Height = 22, Tooltip = text };
    private static NuiListTemplateCell Cell(NuiElement element, float width) => new(element) { Width = width, VariableSize = false };
    private NuiColumn CreateLayout(string portId)
    {
        // Labels have explicit heights; fixed template widths keep text from competing with buttons.
        NuiColumn column = new() { Width = 930 };
        column.Children.Add(Label($"{portId.ToUpperInvariant()} DOCKYARD"));
        column.Children.Add(Label("STORED SHIPS — RETRIEVAL"));
        column.Children.Add(Label(_storedSummary));
        column.Children.Add(new NuiList([
            Cell(Label(_storedNames), 200), Cell(Label(_storedVessels), 170), Cell(Label(_storedStatuses), 280),
            Cell(new NuiButton("RETRIEVE") { Id = "dockyard_retrieve", Enabled = _retrieveEnabled, Height = 28 }, 100),
            Cell(new NuiButton("CARGO / MARKET") { Id = "dockyard_market", Enabled = _marketEnabled, Height = 28 }, 130)
        ], _storedCount) { Width = 920, Height = 130, RowHeight = 34 });
        column.Children.Add(Label("RENTAL OFFERS — FIXED VOYAGE (NOT COMMUNAL RETRIEVAL)"));
        column.Children.Add(Label(_rentalSummary));
        column.Children.Add(new NuiList([
            Cell(new NuiColumn { Children = [Label(_rentalNames), Label(_rentalSpecs), Label(_rentalTerms)] }, 765),
            Cell(new NuiButton("RENT SHIP") { Id = "dockyard_rent", Enabled = _rentEnabled, Height = 28 }, 115)
        ], _rentalCount) { Width = 920, Height = 160, RowHeight = 76 });
        column.Children.Add(Label("YOUR ACTIVE / PENDING RENTAL AGREEMENTS"));
        column.Children.Add(Label(_leaseSummary));
        column.Children.Add(new NuiList([
            Cell(new NuiColumn { Children = [Label(_leaseNames), Label(_leaseTerms)] }, 740),
            Cell(new NuiButton("RETURN RENTAL") { Id = "dockyard_return", Enabled = _returnEnabled, Height = 28 }, 140)
        ], _leaseCount) { Width = 920, Height = 88, RowHeight = 52 });
        column.Children.Add(Label("Terms: fee charged once on successful checkout; no time billing. Retrieval of your existing rental has no new fee."));
        column.Children.Add(Label("Deposit: damage uses the agreement's hull rate, capped at the deposit. No additional gold is taken."));
        column.Children.Add(Label("Return: dock at Southport and unload cargo. Disembarking alone does not end a rental."));
        column.Children.Add(new NuiRow { Height = 34, Children = [new NuiButton("REFRESH") { Id = "shipyard_refresh", Height = 28 },
            new NuiButton("CLOSE") { Id = "ship_retrieval_close", Height = 28 }] });
        return column;
    }

    private void PopulateBinds(Window window)
    {
        var token = window.Token;
        // NuiBind<string> is correct for a list cell: SetBindValues writes the per-row array.
        // Publish counts last, after all parallel arrays from this same immutable snapshot.
        token.SetBindValue(_storedCount, 0); token.SetBindValue(_rentalCount, 0); token.SetBindValue(_leaseCount, 0);
        token.SetBindValues(_storedNames, window.Ships.Rows.Select(r => r.Name));
        token.SetBindValues(_storedVessels, window.Ships.Rows.Select(r => r.Vessel));
        token.SetBindValues(_storedStatuses, window.Ships.Rows.Select(r => r.Status));
        token.SetBindValues(_retrieveEnabled, window.Ships.Rows.Select(r => r.CanRetrieve));
        token.SetBindValues(_marketEnabled, window.Ships.Rows.Select(r => r.CanTrade));
        token.SetBindValue(_storedSummary, window.Ships.Summary);
        token.SetBindValue(_storedCount, window.Ships.Rows.Count);

        var names = new List<string>(); var specs = new List<string>(); var terms = new List<string>(); var enabled = new List<bool>();
        foreach (var definition in window.Rentals)
        {
            var ship = _registry.GetShip(definition.ShipId);
            bool eligible = _rentals.Enabled && ship != null && ShipRentalService.Available(ship, window.Port) && !ship.IsEconomyBusy;
            var offer = DockyardRentalOffer.Create(definition, ship, _rentals.Enabled,
                !eligible || _rentals.ResourcesAvailable(ship!), !eligible || _ports.HasRentalOccupants(ship!));
            names.Add(offer.Name); specs.Add(offer.Specifications); terms.Add(offer.Terms); enabled.Add(offer.CanRent);
        }
        token.SetBindValues(_rentalNames, names); token.SetBindValues(_rentalSpecs, specs); token.SetBindValues(_rentalTerms, terms);
        token.SetBindValues(_rentEnabled, enabled);
        token.SetBindValue(_rentalSummary, !_rentals.Enabled ? "Rental checkout is disabled pending server/resource validation." :
            window.Rentals.Length == 0 ? "No rental vessels are configured at this port." : "Only AVAILABLE FOR RENT offers can start a new voyage agreement.");
        token.SetBindValue(_rentalCount, window.Rentals.Length);

        token.SetBindValues(_leaseNames, window.Leases.Select(lease =>
        {
            ShipState? ship = _registry.GetShip(lease.ShipId);
            string state = ship == null ? "VESSEL MISSING" : ship.IsStored ? $"STORED AT {ship.StoredPortId}" : "ACTIVE IN OCEAN";
            return $"{ship?.ShipName ?? lease.ShipId} — {lease.Status} / {state}";
        }));
        token.SetBindValues(_leaseTerms, window.Leases.Select(lease =>
            $"Fee {lease.Fee}, deposit {lease.Deposit}, damage {lease.DamageRate}/hull; refund owed {lease.GoldOwed} gp. Dock/unload before return."));
        token.SetBindValues(_returnEnabled, window.Leases.Select(lease => lease.Status == ShipRentalStatus.Active &&
            _registry.GetShip(lease.ShipId) is { IsStored: true, IsInitializing: false, IsDocking: false, IsEconomyBusy: false } ship &&
            _storage.IsStoredAtPort(ship, window.Port) && ShipAccessService.Owns(ship, lease.RenterAccount) && ship.Cargo.All(c => c.Quantity == 0) && !_ports.HasRentalOccupants(ship)));
        token.SetBindValue(_leaseSummary, window.Leases.Length == 0 ? "You have no active or pending rental agreements here." :
            "Stored rentals are retrieved above; RETURN settles an agreement and deposit after docking/unloading.");
        token.SetBindValue(_leaseCount, window.Leases.Length);
    }

    private async void HandleEvent(ModuleEvents.OnNuiEvent evt)
    {
        NwPlayer player = evt.Player; string element = evt.ElementId;
        if (!_windows.TryGetValue(player.PlayerName, out var window) || window.Token.Token != evt.Token.Token) return;
        if (evt.EventType == NuiEventType.Close || evt.EventType == NuiEventType.Click && element == "ship_retrieval_close") { Close(player); return; }
        if (evt.EventType != NuiEventType.Click || window.Busy) return;
        if (element == "shipyard_refresh") { Open(player, window.Port); return; }
        window.Busy = true;
        try
        {
            bool retrieved = false; MaritimeResult? result = null;
            if (element is "dockyard_retrieve" or "dockyard_market")
            {
                if (!window.Ships.TryResolve(evt.ArrayIndex, _registry, _storage, window.Port,
                        s => _access.CanRetrieve(s, player, false), out ShipState? ship))
                    result = MaritimeResult.Fail("This stored-ship row is no longer available. Refresh the dockyard list.");
                else if (element == "dockyard_market") { _trade.OpenPort(player, ship!); return; }
                else
                {
                    retrieved = await _ports.RetrieveAsync(ship!, window.Port, player);
                    result = retrieved ? null : MaritimeResult.Fail("This ship cannot be retrieved from your current location. Refresh the dockyard list.");
                }
            }
            else if (element == "dockyard_rent" && evt.ArrayIndex >= 0 && evt.ArrayIndex < window.Rentals.Length)
            {
                var definition = window.Rentals[evt.ArrayIndex];
                result = await _rentals.CheckoutAsync(player, definition.ShipId, window.Port); retrieved = result.Success;
            }
            else if (element == "dockyard_return" && evt.ArrayIndex >= 0 && evt.ArrayIndex < window.Leases.Length)
            {
                var displayed = window.Leases[evt.ArrayIndex];
                var lease = _rentals.ForAccount(MaritimeIdentity.Account(player)).SingleOrDefault(r =>
                    r.RentalId == displayed.RentalId && r.ShipId == displayed.ShipId);
                result = lease == null ? MaritimeResult.Fail("This rental agreement is no longer yours or active.") :
                    await _rentals.ReturnAsync(player, lease.ShipId, window.Port);
            }
            await NwTask.SwitchToMainThread();
            if (!player.IsValid || _disposed) return;
            if (result != null) player.SendServerMessage(result.Message);
            if (_windows.GetValueOrDefault(player.PlayerName) == window)
            { if (retrieved) Close(player); else if (result != null) Open(player, window.Port); }
        }
        catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Shipyard interaction failed."); }
        finally { window.Busy = false; }
    }
    public void Close(NwPlayer player)
    { player.OnNuiEvent -= HandleEvent; if (_windows.Remove(player.PlayerName, out var w)) SailingWindowLifecycle.Close(w.Token); }
    private void HandleDisconnect(ModuleEvents.OnClientLeave evt) => Close(evt.Player);
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        if (_module != null) _module.OnClientLeave -= HandleDisconnect; _module = null;
        foreach (var w in _windows.Values.ToArray()) Close(w.Token.Player);
    }
}
