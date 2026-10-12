using System.Numerics;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Explicit, short-lived travel consent above the existing port/boarding transactions.</summary>
[ServiceBinding(typeof(ShipPartyTransitionService))]
public sealed class ShipPartyTransitionService : IDisposable
{
    private enum Travel { Board, Retrieve, Checkout, Disembark, ShipToShip }
    private sealed record Participant(NwPlayer Player, NwCreature? Creature, string Name);
    private sealed class Session(NwPlayer leader, ShipState ship, Travel travel, NwArea area, Vector3 anchor,
        NwPlaceable? interaction, Dictionary<string, Participant> roster, PartyTravelConsent consent)
    {
        public NwPlayer Leader { get; } = leader;
        public ShipState Ship { get; } = ship;
        public Travel Travel { get; } = travel;
        public NwArea Area { get; } = area;
        public Vector3 Anchor { get; } = anchor;
        public NwPlaceable? Interaction { get; } = interaction;
        public Dictionary<string, Participant> Roster { get; } = roster;
        public PartyTravelConsent Consent { get; } = consent;
        public string? RentalId { get; } = ship.Rental?.RentalId;
        public string? Port { get; init; }
        public ShipLandfall? Landfall { get; init; }
        public ShipBoardingRequest? Boarding { get; init; }
        public bool Invited { get; set; }
        public string[] Selected { get; set; } = [];
        public bool Exit => Travel == Travel.Disembark;
        public string? From => Exit ? Ship.ShipName : Boarding?.RequestingShip.ShipName;
        public string Destination => Exit ? $"{Landfall!.PortName} / {Landfall.AreaResRef} ({Landfall.X},{Landfall.Y},{Landfall.Z})" : Ship.ShipName;
    }
    private sealed record View(Session Session, NuiWindowToken Token, bool Leader);
    public event Action<NwPlayer>? TravelFinished;
    public bool IsTravelPending(string playerName) => _occupied.ContainsKey(playerName);
    private readonly ShipRegistryService _registry;
    private readonly PhysicalShipService _physical;
    private readonly ShipPortService _ports;
    private readonly ShipBoardingService _boarding;
    private readonly ShipRentalService _rentals;
    private readonly ShipAccessService _access;
    private readonly IslandService _islands;
    private readonly MaritimeOptions _options;
    private readonly Dictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Session> _occupied = new(StringComparer.Ordinal);
    private readonly Dictionary<string, View> _views = new(StringComparer.Ordinal);
    private readonly NuiBind<string> _names = new("party_travel_names"), _answers = new("party_travel_answers"),
        _feedback = new("party_travel_feedback"), _partyAction = new("party_travel_action");
    private readonly NuiBind<int> _count = new("party_travel_count");
    private NwModule? _module;
    private bool _disposed;

    public ShipPartyTransitionService(ShipRegistryService registry, PhysicalShipService physical, ShipPortService ports,
        ShipBoardingService boarding, ShipRentalService rentals, ShipAccessService access, IslandService islands, MaritimeOptions options)
    {
        _registry = registry; _physical = physical; _ports = ports; _boarding = boarding;
        _rentals = rentals; _access = access; _islands = islands; _options = options;
        physical.BoardingTravelRequested += HandleBoarding;
        physical.PortTravelRequested += OpenDisembark;
        boarding.AcceptedTravelRequested += OpenAcceptedBoarding;
    }
    internal void InitializeModule(NwModule module)
    { if (_disposed || _module != null) return; _module = module; module.OnClientLeave += HandleDisconnect; }

    private void HandleBoarding(ShipState ship, NwPlayer player, NwPlaceable interaction) => OpenBoarding(player, ship, interaction);
    public void OpenBoarding(NwPlayer player, ShipState ship, NwPlaceable interaction)
    {
        if (ship.IsStored && ship.StoredPortId is { } port) { OpenRetrieval(player, ship, port, interaction); return; }
        Begin(player, ship, Travel.Board, interaction);
    }
    public void OpenRetrieval(NwPlayer player, ShipState ship, string port, NwPlaceable? interaction = null) =>
        Begin(player, ship, Travel.Retrieve, interaction ?? FindShipyard(port, player), port);
    public void OpenCheckout(NwPlayer player, ShipState ship, string port) =>
        Begin(player, ship, Travel.Checkout, FindShipyard(port, player), port);
    public void OpenDisembark(ShipState ship, NwPlayer player)
    {
        if (!_ports.TryPlanDisembark(ship, out var landfall))
        { player.SendServerMessage(ShipDisembarkRules.UnavailableMessage); return; }
        Begin(player, ship, Travel.Disembark, null, landfall: landfall);
    }
    private void OpenAcceptedBoarding(ShipBoardingRequest request)
    {
        var player = _module?.Players.SingleOrDefault(p => p.IsValid && p.PlayerName == request.RequestingPlayerName);
        if (player != null) Begin(player, request.TargetShip, Travel.ShipToShip, null, boarding: request);
        if (player == null || !_sessions.TryGetValue(player.PlayerName, out var session) || session.Boarding != request)
            _boarding.CancelAcceptedTravel(request);
    }
    private NwPlaceable? FindShipyard(string port, NwPlayer player)
    {
        string? tag = _islands.GetIslands().SingleOrDefault(i => i.Id == port)?.ShipyardTag;
        if (string.IsNullOrEmpty(tag)) return null;
        var found = NwObject.FindObjectsWithTag<NwPlaceable>(tag).Where(p => p.IsValid && p.Area == player.ControlledCreature?.Area).ToArray();
        return found.Length == 1 ? found[0] : null;
    }
    private void Begin(NwPlayer player, ShipState ship, Travel travel, NwPlaceable? interaction,
        string? port = null, ShipLandfall? landfall = null, ShipBoardingRequest? boarding = null)
    {
        if (_disposed || _module == null || !player.IsValid) return;
        if (_sessions.ContainsKey(player.PlayerName) || _occupied.ContainsKey(player.PlayerName))
        { player.SendServerMessage("Resolve or cancel your existing sailing travel confirmation first."); return; }
        if (player.LoginCreature is not { IsValid: true, IsLoginPlayerCharacter: true, Area: { IsValid: true } area } creature ||
            player.ControlledCreature != creature || player.IsDM || !_registry.IsCurrent(ship) ||
            (travel is Travel.Board or Travel.Retrieve or Travel.Checkout) && (interaction is not { IsValid: true } || interaction.Area != area))
        { player.SendServerMessage("The boarding interaction or your player character is unavailable."); return; }
        var roster = player.PartyMembers.Prepend(player).Where(p => p.IsValid).DistinctBy(p => p.PlayerName)
            .ToDictionary(p => p.PlayerName, p => new Participant(p, p.LoginCreature, p.LoginCreature?.Name ?? p.PlayerName), StringComparer.Ordinal);
        var consent = new PartyTravelConsent(player.PlayerName, [], DateTime.UtcNow.AddSeconds(_options.PartyConfirmationSeconds));
        var session = new Session(player, ship, travel, area, interaction?.Position ?? creature.Position, interaction, roster, consent)
        { Port = port, Landfall = landfall, Boarding = boarding };
        string? exclusion = Exclusion(session, roster[player.PlayerName]);
        if (exclusion != null || !ContextValid(session))
        { player.SendServerMessage(exclusion ?? "The ship or destination is no longer eligible for travel."); return; }
        var members = roster.Values.Where(p => p.Player != player).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => new PartyTravelMember(p.Player.PlayerName, p.Name, Exclusion(session, p))).ToArray();
        session = new Session(player, ship, travel, area, session.Anchor, interaction, roster,
            new PartyTravelConsent(player.PlayerName, members, consent.Expires)) { Port = port, Landfall = landfall, Boarding = boarding };
        _sessions[player.PlayerName] = session; _occupied[player.PlayerName] = session;
        if (!CreateView(player, session, true)) { Finish(session); return; }
        _ = ExpireAsync(session);
    }
    private bool ContextValid(Session s) => !_disposed && s.Consent.IsOpen(DateTime.UtcNow) && _registry.IsCurrent(s.Ship) &&
        s.Area.IsValid && (s.Interaction == null || s.Interaction.IsValid && s.Interaction.Area == s.Area &&
            Vector3.Distance(s.Interaction.Position, s.Anchor) <= 1f) &&
        (s.Travel == Travel.Checkout || s.Ship.Rental?.RentalId == s.RentalId) && (s.Travel switch
        {
            Travel.Board => s.Ship.IsActiveWorld,
            Travel.Retrieve or Travel.Checkout => s.Ship.IsStored && s.Ship.StoredPortId == s.Port && !s.Ship.IsInitializing &&
                _islands.GetIslands().Any(p => p.Id == s.Port && string.Equals(p.LandingArea, s.Area.ResRef, StringComparison.OrdinalIgnoreCase)),
            Travel.Disembark => s.Ship.IsActiveWorld && _ports.TryPlanDisembark(s.Ship, out var current) && current == s.Landfall,
            Travel.ShipToShip => s.Boarding != null && _boarding.IsAcceptedTravelValid(s.Boarding),
            _ => false
        });
    private bool InParty(Session s, NwPlayer player) => player == s.Leader ||
        s.Leader.IsValid && s.Leader.PartyMembers.Any(p => p == player);
    private bool Authorized(Session s, NwPlayer player) => s.Exit || (s.Travel == Travel.Checkout
        ? player == s.Leader // A new lease has no authorized guests; party membership never creates access.
        : s.Travel == Travel.Retrieve && player == s.Leader ? _access.CanRetrieve(s.Ship, player, false) : _access.CanBoard(s.Ship, player));
    private bool IdentityValid(Participant p) => p.Player.IsValid && !p.Player.IsDM && p.Creature is { IsValid: true, IsLoginPlayerCharacter: true } &&
        p.Player.LoginCreature == p.Creature && p.Player.ControlledCreature == p.Creature;
    private string? Exclusion(Session s, Participant p)
    {
        if (!p.Player.IsValid) return "Offline";
        var creature = p.Player.IsValid ? p.Player.ControlledCreature : null;
        if (!s.Exit && creature?.Area != s.Area) return "Outside boarding range";
        return ShipPartyTravelRules.Exclusion(new(p.Player.IsValid, IdentityValid(p), InParty(s, p.Player),
            creature?.Area?.ResRef, creature?.Position ?? default, _physical.GetShipForPlayer(p.Player.PlayerName),
            s.From == null || _physical.IsPlayerPresent(s.From, p.Player), Authorized(s, p.Player),
            !s.Consent.Started && _physical.IsTransferPending(p.Player.PlayerName)),
            s.From, s.Area.ResRef, s.Anchor, _options.PartyBoardingRadius, s.Exit);
    }
    private bool Guard(Session s) => ContextValid(s) && s.Selected.All(name => s.Roster.TryGetValue(name, out var p) &&
        IdentityValid(p) && InParty(s, p.Player) && Authorized(s, p.Player));
    private bool Departure(Session s, NwPlayer player) => ContextValid(s) &&
        s.Roster.TryGetValue(player.PlayerName, out var p) && p.Player == player && Exclusion(s, p) == null;

    private bool CreateView(NwPlayer player, Session s, bool leader)
    {
        var column = new NuiColumn { Width = 640, Margin = 0, Padding = 0 };
        column.Children.Add(SailingNuiLayout.Text($"{(s.Exit ? "DISEMBARK AT" : "BOARD")} {s.Destination}", 60));
        column.Children.Add(SailingNuiLayout.Text(leader
            ? $"Choose alone, or invite the nearby eligible party members below. Each must accept. Confirmation expires in {_options.PartyConfirmationSeconds} seconds."
            : $"{s.Roster[s.Leader.PlayerName].Name} invites you to travel. ACCEPT authorizes this one transition only; DECLINE keeps you here.", 68));
        if (leader)
        {
            column.Children.Add(new NuiList([SailingNuiLayout.Cell(SailingNuiLayout.Text(_names, 52), 260),
                SailingNuiLayout.Cell(SailingNuiLayout.Text(_answers, 52), 350)], _count)
                { Width = 630, Height = 220, RowHeight = 60 });
            column.Children.Add(new NuiRow { Height = 40, Children = [
                new NuiButton(s.Exit ? "DISEMBARK ALONE" : "BOARD ALONE") { Id = "travel_solo", Height = 34 },
                new NuiButton(_partyAction) { Id = "travel_party", Height = 34 }] });
        }
        else column.Children.Add(new NuiRow { Height = 40, Children = [
            new NuiButton("ACCEPT") { Id = "travel_accept", Height = 34 }, new NuiButton("DECLINE") { Id = "travel_decline", Height = 34 }] });
        column.Children.Add(SailingNuiLayout.Text(_feedback, 60));
        column.Children.Add(new NuiRow { Height = 40, Children = [new NuiButton("REFRESH STATUS") { Id = "travel_refresh", Height = 34 },
            new NuiButton("CANCEL") { Id = "travel_cancel", Height = 34 }] });
        var window = SailingNuiLayout.Window(column, "Sailing Travel Confirmation", 690, leader ? 560 : 340);
        if (!player.TryCreateNuiWindow(window, out var token, "sailing_party_travel")) return false;
        _views[player.PlayerName] = new(s, token, leader);
        player.OnNuiEvent -= HandleEvent; player.OnNuiEvent += HandleEvent;
        token.SetBindValue(_feedback, s.Travel == Travel.Checkout
            ? "Checkout boards the renter. Other accounts need explicit rental guest authorization before boarding; joining a party does not grant access."
            : "No one moves until you confirm. Declined and ineligible members stay where they are.");
        Refresh(s); return true;
    }
    private void Refresh(Session s)
    {
        if (!_views.TryGetValue(s.Leader.PlayerName, out var view)) return;
        view.Token.SetBindValue(_count, 0);
        view.Token.SetBindValues(_names, s.Consent.Members.Select(m => m.DisplayName));
        view.Token.SetBindValues(_answers, s.Consent.Members.Select(m =>
        {
            string? exclusion = m.Exclusion ?? Exclusion(s, s.Roster[m.Player]);
            return m.Answer == PartyTravelAnswer.Declined ? "DECLINED" : exclusion != null ? $"{m.Answer.ToString().ToUpperInvariant()} / {exclusion}" :
                m.Answer == PartyTravelAnswer.Pending ? s.Invited ? "INVITED / WAITING" : "ELIGIBLE / NOT YET INVITED" : m.Answer.ToString().ToUpperInvariant();
        }));
        view.Token.SetBindValue(_count, s.Consent.Members.Count);
        view.Token.SetBindValue(_partyAction, s.Invited ? "CONFIRM PARTY" : s.Exit ? "DISEMBARK WITH PARTY" : "BOARD WITH PARTY");
    }
    private void Invite(Session s)
    {
        s.Invited = true;
        foreach (var member in s.Consent.Members.Where(m => m.Answer == PartyTravelAnswer.Pending))
        {
            var p = s.Roster[member.Player];
            if (Exclusion(s, p) != null || _occupied.ContainsKey(member.Player)) { member.Answer = PartyTravelAnswer.Ineligible; continue; }
            _occupied[member.Player] = s;
            if (!CreateView(p.Player, s, false)) { _occupied.Remove(member.Player); member.Answer = PartyTravelAnswer.Ineligible; }
        }
        Refresh(s);
    }
    private async void HandleEvent(ModuleEvents.OnNuiEvent evt)
    {
        if (!_views.TryGetValue(evt.Player.PlayerName, out var view) || view.Token.Token != evt.Token.Token) return;
        var s = view.Session;
        try
        {
            if (evt.EventType == NuiEventType.Close || evt.EventType == NuiEventType.Click && evt.ElementId == "travel_cancel")
            {
                if (view.Leader || s.Consent.Started) Cancel(s, "Travel cancelled; return to original positions will be attempted if movement has begun.");
                else Answer(evt.Player, s, false);
                return;
            }
            if (evt.EventType != NuiEventType.Click || s.Consent.Started) return;
            if (!ContextValid(s)) { Cancel(s, "Travel expired or its destination changed. Start a new request."); return; }
            if (evt.ElementId == "travel_refresh") { Refresh(s); return; }
            if (!view.Leader)
            {
                if (evt.ElementId is "travel_accept" or "travel_decline") Answer(evt.Player, s, evt.ElementId == "travel_accept");
                return;
            }
            if (evt.ElementId == "travel_party" && !s.Invited) { Invite(s); return; }
            if (evt.ElementId is not ("travel_solo" or "travel_party")) return;
            bool withParty = evt.ElementId == "travel_party";
            if (!s.Consent.TryStart(withParty, DateTime.UtcNow, name => Exclusion(s, s.Roster[name]) == null, out var selected))
            { view.Token.SetBindValue(_feedback, "Waiting for answers, or an accepted participant is no longer eligible. Refresh status or cancel and try again."); Refresh(s); return; }
            s.Selected = selected;
            view.Token.SetBindValue(_feedback, "Transfer in progress. Each arrival is verified before membership changes.");
            bool success = await ExecuteAsync(s);
            await NwTask.SwitchToMainThread();
            foreach (string name in s.Selected)
                if (s.Roster[name].Player is { IsValid: true } p) p.SendServerMessage(success
                    ? $"Travel complete: {s.Destination}." : "Group travel failed. Return to original positions was attempted; no group membership commit occurred.");
            Finish(s);
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Error(ex, "Sailing party transition failed.");
            await NwTask.SwitchToMainThread(); Cancel(s, "Sailing travel failed. Review the transfer log if anyone could not return to their original position.");
            Finish(s);
        }
    }
    private void Answer(NwPlayer player, Session s, bool accept)
    {
        if (accept && (!ContextValid(s) || Exclusion(s, s.Roster[player.PlayerName]) != null))
        { if (_views.TryGetValue(player.PlayerName, out var view)) view.Token.SetBindValue(_feedback, "You are no longer eligible to board here. DECLINE or return to the boarding interaction."); return; }
        if (!s.Consent.Answer(player.PlayerName, accept, DateTime.UtcNow)) return;
        CloseView(player.PlayerName);
        if (!accept) _occupied.Remove(player.PlayerName);
        Refresh(s);
    }
    private async Task<bool> ExecuteAsync(Session s)
    {
        var players = s.Selected.Select(name => s.Roster[name].Player).ToArray();
        if (!Guard(s)) return false;
        switch (s.Travel)
        {
            case Travel.Disembark: return await _ports.DisembarkPartyAsync(s.Ship, players, s.Landfall, () => Guard(s), p => Departure(s, p));
            case Travel.Retrieve: return await _ports.RetrieveAsync(s.Ship, s.Port!, s.Leader, party: players,
                validateParty: () => Guard(s), validateDeparture: p => Departure(s, p));
            case Travel.Checkout:
                var result = await _rentals.CheckoutAsync(s.Leader, s.Ship.ShipId, s.Port!, () => Guard(s), p => Departure(s, p));
                await NwTask.SwitchToMainThread(); if (s.Leader.IsValid) s.Leader.SendServerMessage(result.Message); return result.Success;
            case Travel.ShipToShip: return await _boarding.TransferAcceptedAsync(s.Boarding!, players, () => Guard(s), p => Departure(s, p));
            default:
                return _physical.TryGetBoardingLocation(s.Ship.ShipName, out var destination) && destination != null &&
                    await _physical.TryTransferPlayersAsync(players, null, s.Ship.ShipName, destination, () => Guard(s),
                        validateDeparture: p => Departure(s, p));
        }
    }
    private async Task ExpireAsync(Session s)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(_options.PartyConfirmationSeconds));
        await NwTask.SwitchToMainThread();
        if (!_disposed && !s.Consent.Started && _sessions.GetValueOrDefault(s.Leader.PlayerName) == s)
            Cancel(s, "Sailing travel confirmation expired. No one was moved.");
    }
    private void CloseView(string name)
    {
        if (!_views.Remove(name, out var view)) return;
        view.Token.Player.OnNuiEvent -= HandleEvent;
        SailingWindowLifecycle.Close(view.Token);
    }
    private void Cancel(Session s, string message)
    {
        s.Consent.Cancel();
        foreach (var p in s.Roster.Values.Where(p => p.Player.IsValid && _occupied.GetValueOrDefault(p.Player.PlayerName) == s))
            p.Player.SendServerMessage(message);
        if (!s.Consent.Started) Finish(s);
        else foreach (var pair in _views.Where(p => p.Value.Session == s).ToArray()) CloseView(pair.Key);
    }
    private void Finish(Session s)
    {
        if (s.Boarding != null) _boarding.CancelAcceptedTravel(s.Boarding);
        foreach (var pair in _views.Where(p => p.Value.Session == s).ToArray()) CloseView(pair.Key);
        foreach (var pair in _occupied.Where(p => p.Value == s).ToArray()) _occupied.Remove(pair.Key);
        if (_sessions.GetValueOrDefault(s.Leader.PlayerName) == s) _sessions.Remove(s.Leader.PlayerName);
        // Notify only after the existing transfer/consent cleanup. Observer failures cannot change travel results.
        if (TravelFinished != null)
            foreach (var participant in s.Roster.Values)
                foreach (Action<NwPlayer> observer in TravelFinished.GetInvocationList())
                    try { observer(participant.Player); }
                    catch (Exception ex) { NLog.LogManager.GetCurrentClassLogger().Error(ex, "Travel presentation observer failed."); }
    }
    private void HandleDisconnect(ModuleEvents.OnClientLeave evt)
    {
        if (!_occupied.TryGetValue(evt.Player.PlayerName, out var s)) return;
        if (s.Leader == evt.Player || s.Consent.Started) Cancel(s, "A travel participant disconnected. Group travel cancelled.");
        else Answer(evt.Player, s, false);
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _physical.BoardingTravelRequested -= HandleBoarding; _physical.PortTravelRequested -= OpenDisembark;
        _boarding.AcceptedTravelRequested -= OpenAcceptedBoarding;
        if (_module != null) _module.OnClientLeave -= HandleDisconnect; _module = null;
        foreach (var s in _sessions.Values.ToArray()) { s.Consent.Cancel(); Finish(s); }
    }
}
