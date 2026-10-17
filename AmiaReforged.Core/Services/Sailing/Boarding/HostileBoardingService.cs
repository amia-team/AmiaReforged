using System.Numerics;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(HostileBoardingService))]
public sealed class HostileBoardingService : IHostileBoardingWorld, IDisposable
{
    private sealed class DefenderGroup(ShipState target, NwCreature[] creatures)
    {
        public ShipState Target { get; } = target;
        public NwCreature[] Creatures { get; } = creatures;
        public HashSet<NwCreature> Dead { get; } = [];
    }
    [Inject] public ResourceManager Resources { private get; init; } = null!;
    private readonly ShipRegistryService _registry;
    private readonly PhysicalShipService _physical;
    private readonly ShipEncounterService _encounters;
    private readonly ShipCrewService _crew;
    private readonly ShipAccessService _access;
    private readonly ShipPartyTransitionService _party;
    private readonly ShipSpellEffectStateService _effects;
    private readonly SailingAreaService _areas;
    private readonly MaritimeOptions _options;
    private readonly HostileBoardingEngine _engine;
    private readonly Dictionary<string, DefenderGroup> _defenders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NuiWindowToken> _windows = new(StringComparer.Ordinal);
    private readonly HashSet<string> _recoveryChecks = new(StringComparer.Ordinal);
    private readonly NuiBind<string> _returnStatus = new("hostile_return_status");
    private NwModule? _module;
    private bool _disposed;
    private bool _ticking;
    private const string DefenderTag = "SAILING_HOSTILE_DEFENDER";
    private const string DefenderOwner = "SAILING_HOSTILE_OWNER";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public HostileBoardingService(ShipRegistryService registry, PhysicalShipService physical,
        ShipEncounterService encounters, ShipCrewService crew, ShipAccessService access,
        ShipPartyTransitionService party, ShipSpellEffectStateService effects, SailingAreaService areas,
        MaritimeOptions options, HostileBoardingStore store)
    {
        _registry = registry; _physical = physical; _encounters = encounters; _crew = crew; _access = access;
        _party = party; _effects = effects; _areas = areas; _options = options;
        _engine = new(registry, options.HostileBoarding, this, store);
    }
    internal void InitializeModule(NwModule module)
    {
        if (_disposed || _module != null) return;
        _module = module; module.OnClientLeave += HandleDisconnect;
        // Hot reload recovery only removes creatures stamped by this subsystem.
        foreach (var creature in NwObject.FindObjectsWithTag<NwCreature>(DefenderTag).Where(c => c.IsValid &&
            !string.IsNullOrWhiteSpace(c.GetObjectVariable<LocalVariableString>(DefenderOwner).Value)).ToArray()) creature.Destroy();
    }
    public static string ActorKey(NwPlayer player) => MaritimeIdentity.Hash(MaritimeIdentity.Account(player) + ":" + player.BicFileName);
    private NwPlayer? Player(string actor) => _module?.Players.SingleOrDefault(p => p.IsValid && ActorKey(p) == actor);
    public bool IsShipEngaged(ShipState ship) => _engine.IsShipEngaged(ship);
    public HostileBoardingEligibility Eligibility(NwPlayer player, ShipState source, ShipState? target) =>
        _engine.Check(ActorKey(player), source, target);

    public async Task BoardAsync(NwPlayer player, ShipState source, ShipState? target)
    {
        try
        {
            var result = await _engine.BoardAsync(ActorKey(player), source, target);
            await NwTask.SwitchToMainThread();
            if (player.IsValid)
            {
                player.SendServerMessage(result.Reason);
                string actor = ActorKey(player);
                if (result.Allowed || _engine.HasVisit(actor)) { _recoveryChecks.Add(actor); OpenReturn(player); }
            }
        }
        catch (Exception ex) { Log.Error(ex, "Hostile boarding failed; recovery journal retained when necessary."); }
    }

    public async Task<bool> RecoverPlayerAsync(NwPlayer player)
    {
        if (_disposed || !player.IsValid) return false;
        string actor = ActorKey(player);
        try
        {
            bool recovered = await _engine.ReturnAsync(actor);
            await NwTask.SwitchToMainThread();
            _recoveryChecks.Add(actor);
            if (recovered && player.IsValid) CloseReturn(player);
            return recovered;
        }
        catch (Exception ex) { _recoveryChecks.Remove(actor); Log.Error(ex, "Hostile boarding return could not complete; recovery journal retained."); return false; }
    }

    public async Task TickAsync()
    {
        if (_disposed || _module == null || _ticking) return;
        _ticking = true;
        try
        {
            _engine.Reconcile();
            foreach (NwPlayer player in _module.Players.Where(p => p.IsValid && p.LoginCreature is { IsValid: true }).ToArray())
            {
                try
                {
                    string actor = ActorKey(player);
                    // Also covers characters already online at module restart and failed initial reconnect callbacks.
                    if (_recoveryChecks.Add(actor)) await RecoverPlayerAsync(player);
                    await NwTask.SwitchToMainThread();
                    if (_engine.RequiresRecovery(actor)) await RecoverPlayerAsync(player);
                    await NwTask.SwitchToMainThread();
                    if (_engine.HasVisit(actor) && player.IsValid)
                    {
                        OpenReturn(player);
                        if (_windows.TryGetValue(player.PlayerName, out var token))
                            token.SetBindValue(_returnStatus, _engine.IsResolved(actor)
                                ? "Defenders defeated. The deck encounter is resolved. Use RETREAT to return safely. Cargo remains protected."
                                : "RETREAT is available at any time. Defeat the defenders to resolve the deck encounter. Cargo remains protected.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Boarding recovery/view refresh failed for {0}; other ships continue their tick.", player.PlayerName);
                    CloseReturn(player);
                }
            }
        }
        finally { _ticking = false; }
    }

    Task IHostileBoardingWorld.MainThread() => SwitchMainThread();
    private static async Task SwitchMainThread() => await NwTask.SwitchToMainThread();
    private HostileNpcDeckOption? Profile(ShipState? ship) => ship == null ? null :
        _options.HostileBoarding.NpcDecks.SingleOrDefault(p => p.ShipId == ship.ShipId);
    private static BoardingPoint Point(Location location) => new(location.Area.ResRef,
        location.Position.X, location.Position.Y, location.Position.Z, location.Rotation);
    private Location? LocationAt(BoardingPoint point)
    {
        if (!point.IsValid || _module == null) return null;
        var areas = _module.Areas.Where(a => a.IsValid && a.ResRef.Equals(point.Area, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (areas.Length != 1) return null;
        var location = Location.Create(areas[0], new Vector3(point.X, point.Y, point.Z), point.Facing);
        return location.IsWalkable ? location : null;
    }
    private Location? Waypoint(string tag, string? deck = null)
    {
        if (_module == null || string.IsNullOrWhiteSpace(tag)) return null;
        var points = NwObject.FindObjectsWithTag<NwWaypoint>(tag).Where(w => w.IsValid && w.Area is { IsValid: true } &&
            (deck == null || w.Area.ResRef.Equals(deck, StringComparison.OrdinalIgnoreCase))).ToArray();
        return points.Length == 1 && points[0].Location is { IsWalkable: true } location ? location : null;
    }
    private bool UniqueNpcDeck(ShipState target, HostileNpcDeckOption profile)
    {
        if (_module == null || _areas.TryGetArea(profile.DeckAreaResRef, out _) ||
            _module.Areas.Count(a => a.IsValid && a.ResRef.Equals(profile.DeckAreaResRef, StringComparison.OrdinalIgnoreCase)) != 1 ||
            _registry.GetShips().Any(s => !ReferenceEquals(s, target) &&
                (profile.DeckAreaResRef.Equals(s.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
                 profile.DeckAreaResRef.Equals(s.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))) ||
            _options.RentalVessels.Any(v => profile.DeckAreaResRef.Equals(v.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
                profile.DeckAreaResRef.Equals(v.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))) return false;
        var mapping = _physical.GetPhysicalShip(target.ShipName);
        if (mapping != null && !profile.DeckAreaResRef.Equals(mapping.DeckAreaResRef, StringComparison.OrdinalIgnoreCase)) return false;
        if (mapping != null && !string.IsNullOrWhiteSpace(target.RepresentationTag) && mapping.PlaceableTag != target.RepresentationTag) return false;
        string? tag = mapping?.PlaceableTag ?? target.RepresentationTag;
        return !string.IsNullOrWhiteSpace(tag) && NwObject.FindObjectsWithTag<NwPlaceable>(tag)
            .Count(p => p.IsValid && p.Area?.ResRef.Equals(target.AreaResRef, StringComparison.OrdinalIgnoreCase) == true) == 1;
    }
    private bool DefenderResources(HostileNpcDeckOption profile) => Resources != null &&
        Resources.IsValidResource(profile.DefenderResRef, ResRefType.UTC) &&
        Waypoint(profile.DefenderWaypointTag, profile.DeckAreaResRef) != null;

    HostileBoardingFacts IHostileBoardingWorld.Inspect(string actor, ShipState source, ShipState? target, bool transferring)
    {
        NwPlayer? player = Player(actor);
        var profile = Profile(target);
        bool ownCharacter = player is { IsDM: false, LoginCreature: { IsValid: true, IsLoginPlayerCharacter: true } } &&
            player.ControlledCreature == player.LoginCreature && !player.LoginCreature.IsDead;
        bool aboard = ownCharacter && _physical.GetShipForPlayer(player!.PlayerName) == source.ShipName &&
            (_physical.IsPlayerPresent(source.ShipName, player) || transferring && profile != null &&
             player.LoginCreature!.Area?.ResRef.Equals(profile.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) == true);
        bool authorized = player != null && _access.CanBoard(source, player) &&
            _crew.GetRole(source.ShipName, player.PlayerName) is ShipCrewRole.Captain or ShipCrewRole.Crew;
        float Speed(ShipState ship) => ship.Underway || ship.IsNavigating ?
            _effects.GetMovementMultiplier(ship) * ((int)ship.Heading % 2 == 0 ? 1 : MathF.Sqrt(2)) : 0;
        return new(_registry.IsCurrent(source), _registry.IsCurrent(target), target != null && _encounters.IsInEncounter(source, target),
            aboard, authorized, player == null || _party.IsTravelPending(player.PlayerName) || !transferring && _physical.IsTransferPending(player.PlayerName),
            target != null && profile != null && UniqueNpcDeck(target, profile), profile != null && Waypoint(profile.BoardingWaypointTag, profile.DeckAreaResRef) != null,
            profile != null && DefenderResources(profile) && player?.LoginCreature is { } actorCreature &&
                _defenders.Values.Where(d => ReferenceEquals(d.Target, target)).All(d => d.Creatures
                    .Where(c => c.IsValid && !c.IsDead).All(c => c.IsEnemy(actorCreature))), Deck(source) != null && Emergency() != null,
            Speed(source), target == null ? 0 : Speed(target));
    }
    public BoardingPoint? Deck(ShipState ship)
    {
        if (_module == null) return null;
        if (Profile(ship) is { } profile)
            return UniqueNpcDeck(ship, profile) && Waypoint(profile.BoardingWaypointTag, profile.DeckAreaResRef) is { } npc ? Point(npc) : null;
        return _physical.TryGetBoardingLocation(ship.ShipName, out var location) && location is { IsWalkable: true } ? Point(location) : null;
    }
    public BoardingPoint? Emergency() => Waypoint(_options.HostileBoarding.RecoveryWaypointTag) is { } location && SafeLandfall(Point(location)) ? Point(location) : null;
    private bool SafeLandfall(BoardingPoint point) => LocationAt(point) != null && !_areas.TryGetArea(point.Area, out _) &&
        !_physical.GetPhysicalShips().Any(s => point.Area.Equals(s.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) || point.Area.Equals(s.CabinAreaResRef, StringComparison.OrdinalIgnoreCase)) &&
        !_registry.GetShips().Any(s => point.Area.Equals(s.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) || point.Area.Equals(s.CabinAreaResRef, StringComparison.OrdinalIgnoreCase)) &&
        !_options.HostileBoarding.NpcDecks.Any(d => point.Area.Equals(d.DeckAreaResRef, StringComparison.OrdinalIgnoreCase)) &&
        !_options.RentalVessels.Any(v => point.Area.Equals(v.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) || point.Area.Equals(v.CabinAreaResRef, StringComparison.OrdinalIgnoreCase));

    public bool PrepareDefenders(string actor, ShipState target, out string encounter)
    {
        encounter = "";
        var profile = Profile(target);
        NwPlayer? player = Player(actor);
        if (profile == null || player?.LoginCreature == null || !UniqueNpcDeck(target, profile) || !DefenderResources(profile) ||
            profile.DefenderCount is < 1 || profile.DefenderCount > _options.HostileBoarding.MaximumDefenders ||
            _defenders.Values.Any(d => ReferenceEquals(d.Target, target))) return false;
        var spawn = Waypoint(profile.DefenderWaypointTag, profile.DeckAreaResRef);
        if (spawn == null) return false;
        List<NwCreature> created = [];
        try
        {
            for (int i = 0; i < profile.DefenderCount; i++)
            {
                var defender = NwCreature.Create(profile.DefenderResRef, spawn, false);
                if (defender == null) return false;
                created.Add(defender);
                defender.Tag = DefenderTag;
                defender.GetObjectVariable<LocalVariableString>(DefenderOwner).Value = target.ShipId;
                // Ship classification alone never turns a neutral creature into a hostile defender.
                if (!defender.IsValid || defender.IsDead || defender.IsLoginPlayerCharacter || !defender.IsEnemy(player.LoginCreature)) return false;
            }
            if (!_physical.HasPhysicalShip(target.ShipName))
            {
                var definition = new ShipDefinition(target.ShipName, target.SpritePrefix, "", target.RepresentationTag!, "",
                    profile.DeckAreaResRef, "", target.AreaResRef, target.X, target.Y, target.Z, target.Heading, target.ShipType, target.MaximumHull);
                _physical.RegisterPhysicalShip(definition);
                target.DeckAreaResRef = profile.DeckAreaResRef;
            }
            foreach (var creature in created) creature.OnDeath += HandleDefenderDeath;
            encounter = Guid.NewGuid().ToString("N");
            _defenders.Add(encounter, new(target, created.ToArray()));
            return true;
        }
        finally
        {
            if (encounter.Length == 0) foreach (var creature in created)
                if (creature.IsValid) { creature.OnDeath -= HandleDefenderDeath; creature.Destroy(); }
        }
    }
    private void HandleDefenderDeath(CreatureEvents.OnDeath evt)
    { foreach (var group in _defenders.Values) if (group.Creatures.Contains(evt.KilledCreature)) group.Dead.Add(evt.KilledCreature); }
    public BoardingDefenderState Defenders(string encounter)
    {
        if (!_defenders.TryGetValue(encounter, out var group)) return BoardingDefenderState.Unavailable;
        if (group.Creatures.All(c => group.Dead.Contains(c) || c.IsValid && c.IsDead)) return BoardingDefenderState.Defeated;
        return group.Creatures.Any(c => !group.Dead.Contains(c) && (!c.IsValid || c.Area?.ResRef != Profile(group.Target)?.DeckAreaResRef)) ?
            BoardingDefenderState.Unavailable : BoardingDefenderState.Fighting;
    }
    public void CleanupDefenders(string encounter)
    {
        if (!_defenders.Remove(encounter, out var group)) return;
        foreach (var creature in group.Creatures)
            if (creature.IsValid) { creature.OnDeath -= HandleDefenderDeath; creature.Destroy(); }
    }
    public bool OnShip(string actor, ShipState ship) => Player(actor) is { } player && _physical.IsPlayerPresent(ship.ShipName, player);
    public bool CanRecover(string actor, HostileBoardingVisit visit) => Player(actor)?.LoginCreature?.Area is { IsValid: true } area &&
        (area.ResRef.Equals(visit.Target.Area, StringComparison.OrdinalIgnoreCase) || area.ResRef.Equals(visit.Origin.Area, StringComparison.OrdinalIgnoreCase) ||
         area.ResRef.Equals(visit.TargetCabinArea, StringComparison.OrdinalIgnoreCase) || area.ResRef.Equals(visit.OriginCabinArea, StringComparison.OrdinalIgnoreCase));
    public bool SafelyOutsideVisit(string actor, HostileBoardingVisit visit) => Player(actor) is { } player &&
        player.LoginCreature is { IsValid: true, Area: { IsValid: true }, Location: { } location } &&
        player.ControlledCreature == player.LoginCreature && !CanRecover(actor, visit) && SafeLandfall(Point(location));
    public bool AtPoint(string actor, BoardingPoint point) => Player(actor)?.LoginCreature is { IsValid: true } creature &&
        creature.Area?.ResRef.Equals(point.Area, StringComparison.OrdinalIgnoreCase) == true &&
        Vector3.Distance(creature.Position, new Vector3(point.X, point.Y, point.Z)) <= 3;
    public BoardingPoint? ReturnDestination(string actor, HostileBoardingVisit visit, out string? ship)
    {
        ship = null;
        NwPlayer? player = Player(actor);
        ShipState? origin = _registry.GetShip(visit.OriginId);
        if (player == null) return null;
        if (origin is { ShipType: ShipType.Player, IsActiveWorld: true, IsDisabled: false } && origin.Rental?.RentalId == visit.RentalId &&
            _access.CanBoard(origin, player) && Deck(origin) is { } deck)
        { ship = origin.ShipName; return deck; }
        return SafeLandfall(visit.Recovery) ? visit.Recovery : Emergency();
    }
    public async Task<bool> Transfer(string actor, string? fromShip, string? toShip, BoardingPoint point, Func<bool> validate, bool recovery)
    {
        NwPlayer? player = Player(actor);
        var location = LocationAt(point);
        if (player == null || location == null || _disposed || player.LoginCreature is not { IsValid: true, IsLoginPlayerCharacter: true } ||
            player.ControlledCreature != player.LoginCreature || _party.IsTravelPending(player.PlayerName)) return false;
        // Recover against actual tracked membership, including stale/disappeared ships.
        if (recovery) fromShip = _physical.GetShipForPlayer(player.PlayerName);
        return await _physical.TryTransferPlayersAsync([player], fromShip, toShip, location, validate,
            allowUnavailableSource: recovery);
    }
    public bool Checkpoint(string actor)
    {
        try
        {
            var player = Player(actor);
            if (player?.LoginCreature is not { IsValid: true, Area: { IsValid: true } } || player.ControlledCreature != player.LoginCreature) return false;
            player.ExportCharacter(); return true;
        }
        catch (Exception ex) { Log.Error(ex, "Boarding character export failed; durable recovery record retained."); return false; }
    }
    private void OpenReturn(NwPlayer player)
    {
        if (_disposed || _windows.ContainsKey(player.PlayerName)) return;
        NuiColumn root = new() { Children = [new NuiLabel(_returnStatus) { Height = 65 },
            new NuiButton("RETREAT TO ORIGIN / SAFE LANDFALL") { Id = "hostile_retreat", Height = 35 }] };
        NuiWindow window = new(root, "Hostile boarding") { Geometry = new NuiRect(-1, -1, 440, 155), Closable = false };
        if (player.TryCreateNuiWindow(window, out var token, "hostile_boarding_return"))
        { _windows[player.PlayerName] = token; player.OnNuiEvent -= HandleReturn; player.OnNuiEvent += HandleReturn; }
    }
    private void HandleReturn(ModuleEvents.OnNuiEvent evt)
    {
        if (!_windows.TryGetValue(evt.Player.PlayerName, out var token) || token.Token != evt.Token.Token) return;
        if (evt.EventType == NuiEventType.Close) { CloseReturn(evt.Player); return; }
        if (evt.EventType == NuiEventType.Click && evt.ElementId == "hostile_retreat") _ = RecoverPlayerAsync(evt.Player);
    }
    private void CloseReturn(NwPlayer player)
    {
        player.OnNuiEvent -= HandleReturn;
        if (_windows.Remove(player.PlayerName, out var token)) SailingWindowLifecycle.Close(token);
    }
    private void HandleDisconnect(ModuleEvents.OnClientLeave evt)
    { _recoveryChecks.Remove(ActorKey(evt.Player)); CloseReturn(evt.Player); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_module is { IsValid: true })
        {
            _module.OnClientLeave -= HandleDisconnect;
            foreach (NwPlayer player in _module.Players.Where(p => p.IsValid).ToArray()) CloseReturn(player);
            _engine.DisposeDefenders();
        }
        _module = null;
    }
}
