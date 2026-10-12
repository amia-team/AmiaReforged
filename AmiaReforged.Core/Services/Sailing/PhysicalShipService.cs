using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(PhysicalShipService))]
public class PhysicalShipService : IDisposable
{
    private const float DeckSpawnX = 43.0f;

    private const float DeckSpawnY = 43.0f;

    private const float DeckSpawnZ = 0.0f;

    private const float DeckSpawnRotation = 0.0f;
    private const string RentalVisitVariable = "SAILING_RENTAL_VISIT";

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, PhysicalShip>
        _physicalShips = new();

    // -----------------------------------------------------------------
    // Aboard Player Tracking
    // -----------------------------------------------------------------

    private readonly Dictionary<string, HashSet<string>>
        _aboardPlayers = new();

    // -----------------------------------------------------------------
    // Aboard Player Events
    // -----------------------------------------------------------------

    public event Action<string, NwPlayer>? PlayerBoarded;

    public event Action<string, NwPlayer>? PlayerLeft;
    public event Action<string, string>? PlayerDetached;
    public event Func<ShipState, NwPlayer, Task<bool>>? StoredExitRequested;
    public event Func<ShipState, NwPlayer, Task<bool>>? PortDisembarkRequested;
    private readonly ShipRegistryService _registry;
    private readonly ShipAccessService? _access;
    private readonly Dictionary<string, string> _representationTags = new();
    private readonly Dictionary<string, List<NwPlaceable>> _representations = new();
    private readonly Dictionary<NwPlaceable, VisibilityMode> _hidden = new();
    private readonly Dictionary<string, NwArea> _areas = new(StringComparer.OrdinalIgnoreCase);
    private NwModule? _module;
    private bool _disposed;
    private readonly ShipTransferReservations _transfers = new();

    public bool CanCommandShip(ShipState ship, NwPlayer player) => _access?.CanCommand(ship, player) ??
        ShipAccessService.MayCommand(ship, MaritimeIdentity.Account(player));

    public bool IsTransferPending(string playerName) => _transfers.Contains(playerName);

    public PhysicalShipService(ShipRegistryService registry, ShipAccessService? access = null)
    {
        _registry = registry; _access = access;
    }

    internal void InitializeModule(NwModule module)
    {
        if (_disposed || _module != null) return;
        module.OnClientLeave += HandleClientLeave;
        _module = module;
    }

    // -----------------------------------------------------------------
    // Registration
    // -----------------------------------------------------------------

    public void RegisterPhysicalShip(ShipDefinition definition)
    {
        var interiors = new[] { definition.DeckAreaResRef, definition.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a));
        if (_physicalShips.Values.Any(p => p.ShipName != definition.ShipName &&
            (definition.DefinitionId == CutterRentalFleet.DefinitionId || _registry.GetShip(p.ShipName) is { } other && CutterRentalFleet.IsCutter(other)) &&
            (new[] { p.DeckAreaResRef, p.CabinAreaResRef }.Intersect(interiors, StringComparer.OrdinalIgnoreCase).Any() ||
             new[] { p.HelmPlaceableTag, p.ExitPlaceableTag, p.PlaceableTag }.Intersect(
                 new[] { definition.HelmTag, definition.ExitTag, definition.PlaceableTag }, StringComparer.OrdinalIgnoreCase).Any())))
            throw new InvalidOperationException("Rental cutter physical mappings must not share interiors or interaction tags.");
        PhysicalShip ship = new()
        {
            ShipName = definition.ShipName,
            PlaceableTag = definition.PlaceableTag,
            ExitPlaceableTag = definition.ExitTag,
            HelmPlaceableTag = definition.HelmTag,
            DeckAreaResRef = definition.DeckAreaResRef,
            CabinAreaResRef = definition.CabinAreaResRef
        };

        _physicalShips[ship.ShipName] = ship;
        RegisterRepresentation(definition.ShipName, definition.PlaceableTag);
        RegisterPhysicalShipInteractions();

        //Log.Info(
        //$"Registered physical ship: " +
        //$"Ship={ship.ShipName}, " +
        //$"Tag={ship.PlaceableTag}, " +
        //$"Deck={ship.DeckAreaResRef}, " +
        //$"Cabin={ship.CabinAreaResRef}");
    }
    // -----------------------------------------------------------------
    // Physical Ship Interaction Registration
    // -----------------------------------------------------------------

    public void RegisterPhysicalShipInteractions()
    {
        if (_disposed || _module == null) return;
        foreach (PhysicalShip physicalShip
            in _physicalShips.Values)
        {
            List<NwPlaceable> shipPlaceables =
                FindShipPlaceables(
                    physicalShip.ShipName);

            foreach (NwPlaceable placeable
                in shipPlaceables)
            {
                placeable.OnLeftClick -= HandlePhysicalShipClick;
                placeable.OnLeftClick +=
                    HandlePhysicalShipClick;

                //Log.Info(
                //$"Physical ship interaction registered: " +
                //$"Ship={physicalShip.ShipName}, " +
                //$"Tag={placeable.Tag}, " +
                //$"ResRef={placeable.ResRef}");
            }

            if (string.IsNullOrWhiteSpace(physicalShip.ExitPlaceableTag)) continue;
            List<NwPlaceable> exitPlaceables =
                NwObject.FindObjectsWithTag<NwPlaceable>(
                    physicalShip.ExitPlaceableTag)
                .ToList();

            foreach (NwPlaceable exitPlaceable
                in exitPlaceables)
            {
                exitPlaceable.OnLeftClick -= HandleShipExitClick;
                exitPlaceable.OnLeftClick +=
                    HandleShipExitClick;

                //Log.Info(
                //$"Ship exit registered: " +
                //$"Ship={physicalShip.ShipName}, " +
                //$"Tag={exitPlaceable.Tag}, " +
                //$"ResRef={exitPlaceable.ResRef}");
            }
        }
    }

    // -----------------------------------------------------------------
    // Lookup
    // -----------------------------------------------------------------

    public PhysicalShip? GetPhysicalShip(
        string shipName)
    {
        if (_physicalShips.TryGetValue(
                shipName,
                out PhysicalShip? physicalShip))
        {
            return physicalShip;
        }

        Log.Debug("Physical ship '{0}' is not registered; abstract NPCs may have no physical ship.", shipName);

        return null;
    }

    public IReadOnlyCollection<PhysicalShip>
        GetPhysicalShips()
    {
        return _physicalShips.Values;
    }

    public bool HasPhysicalShip(
        string shipName)
    {
        return _physicalShips.ContainsKey(
            shipName);
    }

    // -----------------------------------------------------------------
    // Physical Ship Lookup
    // -----------------------------------------------------------------

    public List<NwPlaceable> FindShipPlaceables(
        string shipName)
    {
        PhysicalShip? physicalShip =
            GetPhysicalShip(shipName);

        if (physicalShip == null)
        {
            return new List<NwPlaceable>();
        }

        List<NwObject> objects =
            NwObject.FindObjectsWithTag(
                physicalShip.PlaceableTag)
            .ToList();

        List<NwPlaceable> placeables =
            objects
                .OfType<NwPlaceable>()
                .ToList();

        //Log.Info(
        //$"Physical ship lookup: " +
        //$"Ship={shipName}, " +
        //$"Tag={physicalShip.PlaceableTag}, " +
        //$"Found={placeables.Count}");

        foreach (NwPlaceable placeable
            in placeables)
        {
            //Log.Info(
            //$"Physical ship found: " +
            //$"Ship={shipName}, " +
            //$"Tag={placeable.Tag}, " +
            //$"ResRef={placeable.ResRef}");
        }

        return placeables;
    }

    public NwPlaceable? FindShipPlaceable(
        string shipName)
    {
        List<NwPlaceable> placeables =
            FindShipPlaceables(
                shipName);

        if (placeables.Count == 0)
        {
            Log.Debug(
                $"Physical ship not found: " +
                $"Ship={shipName}");

            return null;
        }

        return placeables[0];
    }

    // -----------------------------------------------------------------
    // Aboard Player Tracking
    // -----------------------------------------------------------------

    public bool AddPlayerAboard(string shipName, NwPlayer player) => AddPlayerAboard(shipName, player, recovering: false);

    private bool AddPlayerAboard(string shipName, NwPlayer player, bool recovering)
    {
        if (IsTransferPending(player.PlayerName) || !HasPhysicalShip(shipName) || _registry.GetShip(shipName) is not { IsActiveWorld: true } aboardShip ||
            (recovering && CutterRentalFleet.IsCutter(aboardShip) && !ShipAccessService.MayRecoverRentalVisit(aboardShip,
                MaritimeIdentity.Account(player), player.LoginCreature?.GetObjectVariable<LocalVariableString>(RentalVisitVariable).Value)) ||
            (_access != null && !(recovering
                ? ShipAccessService.MayRecoverMembership(aboardShip, MaritimeIdentity.Account(player),
                    GetPhysicalShip(shipName) is { } context && ShipCrewLifecycle.MatchesExit(shipName, shipName,
                        player.ControlledCreature?.Area?.ResRef, player.ControlledCreature?.Area?.ResRef, context.DeckAreaResRef, context.CabinAreaResRef))
                : _access.CanBoard(aboardShip, player))))
        {
            Log.Warn(
                $"Cannot add player '{player.PlayerName}' " +
                $"aboard '{shipName}': " +
                "physical ship does not exist.");

            return false;
        }

        string? existingShip =
            GetShipForPlayer(
                player.PlayerName);

        if (existingShip != null)
        {
            if (string.Equals(
                    existingShip,
                    shipName,
                    StringComparison.Ordinal))
            {
                //Log.Info(
                //$"Player {player.PlayerName} " +
                //$"is already aboard {shipName}.");

                return true;
            }

            Log.Warn(
                $"Player {player.PlayerName} " +
                $"is already aboard '{existingShip}' " +
                $"and cannot be added to '{shipName}'.");

            return false;
        }

        if (!_aboardPlayers.TryGetValue(
                shipName,
                out HashSet<string>? players))
        {
            players =
                new HashSet<string>(
                    StringComparer.Ordinal);

            _aboardPlayers[
                shipName] =
                players;
        }

        players.Add(
            player.PlayerName);
        if (!recovering) StampRentalVisit(player, aboardShip);

        //Log.Info(
        //$"Player {player.PlayerName} " +
        //$"is now aboard {shipName}. " +
        //$"Aboard={players.Count}");

        // Notify interested services that the player
        // has successfully boarded the ship.
        Notify(PlayerBoarded, shipName, player);

        return true;
    }

    public bool RemovePlayerAboard(
        string shipName,
        NwPlayer player)
    {
        if (!_aboardPlayers.TryGetValue(
                shipName,
                out HashSet<string>? players))
        {
            //Log.Info(
            //$"Player {player.PlayerName} " +
            //$"was not being tracked aboard " +
            //$"{shipName}.");

            return false;
        }

        bool removed =
            players.Remove(
                player.PlayerName);

        if (removed)
        {
            if (!IsInInterior(shipName, player.ControlledCreature?.Area)) StampRentalVisit(player, null);
            //Log.Info(
            //$"Player {player.PlayerName} " +
            //$"left {shipName}. " +
            //$"Aboard={players.Count}");

            Notify(PlayerDetached, shipName, player.PlayerName);

            // Notify interested services that the player
            // has successfully left the ship.
            Notify(PlayerLeft, shipName, player);

            if (players.Count == 0)
            {
                _aboardPlayers.Remove(
                    shipName);
            }
        }

        return removed;
    }

    public bool IsPlayerAboard(
        string shipName,
        string playerName)
    {
        return _aboardPlayers.TryGetValue(
                   shipName,
                   out HashSet<string>? players) &&
               players.Contains(
                   playerName);
    }

    public IReadOnlyCollection<string>
        GetPlayersAboard(
            string shipName)
    {
        if (!_aboardPlayers.TryGetValue(
                shipName,
                out HashSet<string>? players))
        {
            return Array.Empty<string>();
        }

        return players.ToArray();
    }

    public string? GetShipForPlayer(
        string playerName)
    {
        foreach (
            KeyValuePair<string, HashSet<string>>
                entry in _aboardPlayers)
        {
            if (entry.Value.Contains(
                    playerName))
            {
                return entry.Key;
            }
        }

        return null;
    }

    private static void Notify<T>(Action<string, T>? callbacks, string ship, T player)
    {
        if (callbacks == null) return;
        foreach (Action<string, T> callback in callbacks.GetInvocationList())
            try { callback(ship, player); } catch (Exception ex) { Log.Error(ex, "Ship lifecycle subscriber failed."); }
    }

    public async Task<bool> TryDisembarkAllAsync(string shipName, Location destination,
        System.Action beforeCommit, Func<bool> validateShip)
    {
        if (_disposed || destination.Area is not { IsValid: true } || !validateShip()) return false;
        var participants = new List<(NwPlayer Player, NwCreature Creature, Location Previous)>();
        foreach (string name in GetPlayersAboard(shipName))
        {
            NwPlayer? player = NwModule.Instance.Players.FirstOrDefault(p => p.IsValid && p.PlayerName == name);
            if (player?.ControlledCreature is not { IsValid: true } creature ||
                creature.Location is not { } previous || !IsPlayerPresent(shipName, player)) return false;
            participants.Add((player, creature, previous));
        }
        using IDisposable? reservation = _transfers.TryReserve(participants.Select(p => p.Player.PlayerName).ToArray());
        if (reservation == null) return false;
        bool committed = false;
        try
        {
            void ValidateParty()
            {
                if (!validateShip() || !GetPlayersAboard(shipName).ToHashSet(StringComparer.Ordinal)
                        .SetEquals(participants.Select(p => p.Player.PlayerName)))
                    throw new InvalidOperationException("Docking ship or crew changed during transfer.");
            }
            var moves = participants.Select(entry =>
            {
                void Validate()
                {
                    ValidateParticipant(entry.Player, entry.Creature);
                    ValidateParty();
                    if (GetShipForPlayer(entry.Player.PlayerName) != shipName)
                        throw new InvalidOperationException("Aboard membership changed during docking.");
                }
                return ((Func<Task>)(() => MoveCreatureAsync(entry.Player, entry.Creature, destination, Validate, false)),
                    (Func<Task>)(() => MoveCreatureAsync(entry.Player, entry.Creature, entry.Previous,
                        () => ValidateRollback(entry.Player, entry.Creature), true)),
                    (System.Action)(() => { Validate(); RequireArrival(entry.Creature, destination); }));
            }).ToArray();
            committed = await ShipTransferTransaction.ExecuteAsync(moves, () =>
            {
                ValidateParty();
                beforeCommit();
                foreach (var entry in participants) RemovePlayerAboard(shipName, entry.Player);
            }, ex => Log.Warn(ex, "Docking transfer failed; rollback attempted for {0}.", shipName));
            Log.Info("Ship transfer transaction {0}: docking ship={1}, players={2}.",
                committed ? "COMMIT" : "ROLLBACK ATTEMPTED", shipName, string.Join(",", participants.Select(p => p.Player.PlayerName)));
            return committed;
        }
        finally
        {
            if (!committed && !_disposed)
                foreach (var entry in participants) RemoveStrandedMembership(shipName, entry.Player);
        }
    }

    public void RegisterRepresentation(string shipName, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return; // Abstract NPCs are intentional.
        _representationTags[shipName] = tag;
        _representations.Remove(shipName);
    }

    public void EnsureRepresentation(ShipState ship, string resRef, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(resRef)) return;
        RegisterRepresentation(ship.ShipName, tag);
        if (!NwObject.FindObjectsWithTag<NwPlaceable>(tag).Any())
        {
            NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == ship.AreaResRef);
            if (area == null) return;
            NwPlaceable? placeable = NwPlaceable.Create(resRef,
                Location.Create(area, new System.Numerics.Vector3(ship.X, ship.Y, ship.Z), (float)ShipActionRules.HeadingDegrees(ship.Heading)), false);
            if (placeable == null) return;
            placeable.Tag = tag;
        }
        Synchronize(ship);
    }

    public void Synchronize(ShipState ship)
    {
        if (!_representationTags.TryGetValue(ship.ShipName, out string? tag)) return;
        if (!_representations.TryGetValue(ship.ShipName, out List<NwPlaceable>? objects) || objects.Any(o => !o.IsValid))
            _representations[ship.ShipName] = objects = NwObject.FindObjectsWithTag<NwPlaceable>(tag).Where(o => o.IsValid).ToList();
        if (!_areas.TryGetValue(ship.AreaResRef, out NwArea? area) || !area.IsValid)
        {
            area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == ship.AreaResRef);
            if (area != null) _areas[ship.AreaResRef] = area;
        }
        foreach (NwPlaceable placeable in objects)
        {
            if (ship.IsStored)
            {
                if (!_hidden.ContainsKey(placeable)) _hidden[placeable] = placeable.VisibilityOverride;
                placeable.VisibilityOverride = VisibilityMode.Hidden;
                continue;
            }
            if (_hidden.Remove(placeable, out var original))
            { placeable.VisibilityOverride = original; }
            if (area != null)
                placeable.Location = Location.Create(area, new System.Numerics.Vector3(ship.X, ship.Y, ship.Z),
                    (float)ShipActionRules.HeadingDegrees(ship.Heading));
        }
    }

    public bool IsPlayerPresent(string shipName, NwPlayer player)
    {
        PhysicalShip? physical = GetPhysicalShip(shipName);
        NwCreature? creature = player.ControlledCreature;
        return physical != null && player.IsValid && creature is { IsValid: true } &&
            IsPlayerAboard(shipName, player.PlayerName) &&
            (string.Equals(creature.Area?.ResRef, physical.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(creature.Area?.ResRef, physical.CabinAreaResRef, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Return/checkout must also detect real occupants omitted from transient membership (e.g. reconnect).</summary>
    public bool HasDeckOccupants(ShipState ship)
    {
        if (GetPlayersAboard(ship.ShipName).Any()) return true;
        var mapping = GetPhysicalShip(ship.ShipName);
        if (mapping == null || _module == null) return true;
        return NwModule.Instance.Players.Any(p => p.IsValid && (IsInInterior(ship.ShipName, p.ControlledCreature?.Area) ||
            IsInInterior(ship.ShipName, p.LoginCreature?.Area)));
    }
    public bool IsInInterior(string shipName, NwArea? area) => area != null && GetPhysicalShip(shipName) is { } p &&
        (area.ResRef.Equals(p.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(p.CabinAreaResRef) &&
         area.ResRef.Equals(p.CabinAreaResRef, StringComparison.OrdinalIgnoreCase));

    private static void StampRentalVisit(NwPlayer player, ShipState? ship)
    {
        // Persist a lease generation, so a returning offline passenger cannot attach to a later renter's voyage.
        try
        {
            if (player.LoginCreature is not { IsValid: true } creature) return;
            var stamp = creature.GetObjectVariable<LocalVariableString>(RentalVisitVariable);
            string value = ship != null && CutterRentalFleet.IsCutter(ship) ? ship.Rental?.RentalId ?? "" : "";
            if ((stamp.Value ?? "") == value) return;
            stamp.Value = value; player.ExportCharacter();
        }
        catch (Exception ex) { Log.Warn(ex, "Rental visit checkpoint failed for {0}; reconnect must validate the current agreement before restoring membership.", player.PlayerName); }
    }

    public bool RecoverPlayerMembership(NwPlayer player)
    {
        if (!player.IsValid || IsTransferPending(player.PlayerName) ||
            player.ControlledCreature is not { IsValid: true, Area: { } area }) return false;
        string? tracked = GetShipForPlayer(player.PlayerName);
        if (tracked != null) return IsPlayerPresent(tracked, player);
        PhysicalShip[] matches = _physicalShips.Values.Where(physical =>
            _registry.GetShip(physical.ShipName) is { IsActiveWorld: true } &&
            (string.Equals(area.ResRef, physical.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(area.ResRef, physical.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))).ToArray();
        // Shared/ambiguous decks cannot assign a player to an arbitrary first ship.
        return matches.Length == 1 && AddPlayerAboard(matches[0].ShipName, player, recovering: true);
    }

    public bool TryGetBoardingLocation(string shipName, out Location? location)
    {
        location = null;
        PhysicalShip? ship = GetPhysicalShip(shipName);
        if (ship == null || !_registry.IsCurrent(_registry.GetShip(shipName))) return false;
        NwArea[] decks = NwModule.Instance.Areas.Where(a => a.IsValid &&
            string.Equals(a.ResRef, ship.DeckAreaResRef, StringComparison.OrdinalIgnoreCase)).ToArray();
        NwArea? deck = decks.Length == 1 ? decks[0] : null;
        if (deck == null)
        {
            Log.Warn("Ship deck unavailable: ship={0}, deck={1}.", shipName, ship.DeckAreaResRef);
            return false;
        }
        var spawns = NwObject.FindObjectsWithTag<NwWaypoint>("SAILING_DECK_SPAWN").Where(w => w.IsValid && w.Area == deck).ToArray();
        if (_registry.GetShip(shipName) is { } state && CutterRentalFleet.IsCutter(state) && spawns.Length != 1)
        { Log.Warn("Cutter deck requires exactly one SAILING_DECK_SPAWN: ship={0}, deck={1}, found={2}.", shipName, deck.ResRef, spawns.Length); return false; }
        location = spawns.FirstOrDefault()?.Location;
        Log.Info("Ship boarding destination: ship={0}, deck={1}, spawn={2}.", shipName,
            deck.ResRef, location == null ? "fallback (43,43,0)" : "SAILING_DECK_SPAWN");
        // Reuse the original boarding coordinates when no waypoint is present.
        // Retrieval and consensual boarding must use the same existing entry policy.
        location ??= Location.Create(deck, new System.Numerics.Vector3(DeckSpawnX, DeckSpawnY, DeckSpawnZ), DeckSpawnRotation);
        return true;
    }

    public async Task<bool> TryTransferPlayerAsync(NwPlayer player, string? fromShip, string? toShip,
        Location destination, Func<bool>? validateAction = null, System.Action? beforeCommit = null,
        bool allowStoredTarget = false, Func<Task>? prepareCommit = null, Func<Task>? abortCommit = null)
    {
        NwCreature? creature = player.ControlledCreature;
        ShipState? sourceShip = fromShip == null ? null : _registry.GetShip(fromShip);
        ShipState? targetShip = toShip == null ? null : _registry.GetShip(toShip);
        bool TargetEligible() => toShip == null || (HasPhysicalShip(toShip) && _registry.IsCurrent(targetShip) &&
            (_access == null || _access.CanBoard(targetShip!, player) || allowStoredTarget && _access.CanRetrieve(targetShip!, player, true)) &&
            (targetShip!.IsActiveWorld || (allowStoredTarget && targetShip.IsStored && targetShip.IsDocking && !targetShip.IsInitializing)));
        if (_disposed || !player.IsValid || creature is not { IsValid: true } || destination.Area is not { IsValid: true } ||
            GetShipForPlayer(player.PlayerName) != fromShip ||
            (fromShip != null && (!IsPlayerPresent(fromShip, player) || sourceShip is not { IsActiveWorld: true })) ||
            !TargetEligible() || validateAction?.Invoke() == false) return false;
        if (toShip != null && GetPhysicalShip(toShip) is { } target &&
            !string.Equals(destination.Area.ResRef, target.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(destination.Area.ResRef, target.CabinAreaResRef, StringComparison.OrdinalIgnoreCase)) return false;
        Location? previous = creature.Location;
        if (previous == null) return false;
        using IDisposable? reservation = _transfers.TryReserve([player.PlayerName]);
        if (reservation == null) return false;
        bool committed = false;
        try
        {
            void Validate()
            {
                ValidateParticipant(player, creature);
                if (GetShipForPlayer(player.PlayerName) != fromShip ||
                    (fromShip != null && (!_registry.IsCurrent(sourceShip) || !sourceShip!.IsActiveWorld)) ||
                    !TargetEligible() || validateAction?.Invoke() == false)
                    throw new InvalidOperationException("Ship or membership changed during transfer.");
            }
            committed = await ShipTransferTransaction.ExecuteAsync([
                (() => MoveCreatureAsync(player, creature, destination, Validate, false),
                 () => MoveCreatureAsync(player, creature, previous, () => ValidateRollback(player, creature), true),
                 () => { Validate(); RequireArrival(creature, destination); })],
                () => { beforeCommit?.Invoke(); CommitTransfer(player, fromShip, toShip); },
                ex => Log.Warn(ex, "Ship transfer failed; rollback attempted: player={0}, from={1}, to={2}.",
                    player.PlayerName, fromShip, toShip), prepareCommit, abortCommit);
            Log.Info("Ship transfer transaction {0}: player={1}, from={2}, to={3}.",
                committed ? "COMMIT" : "ROLLBACK ATTEMPTED", player.PlayerName, fromShip, toShip);
            return committed;
        }
        finally
        {
            if (!committed && !_disposed && fromShip != null) RemoveStrandedMembership(fromShip, player);
        }
    }

    private void ValidateParticipant(NwPlayer player, NwCreature creature)
    {
        ValidateRollback(player, creature);
        if (player.ControlledCreature != creature)
            throw new InvalidOperationException("Player changed controlled creature during transfer.");
    }

    private void ValidateRollback(NwPlayer player, NwCreature creature)
    {
        if (_disposed || !player.IsValid || !creature.IsValid)
            throw new OperationCanceledException("Ship transfer participant disconnected or service stopped.");
    }

    private static void RequireArrival(NwCreature creature, Location destination)
    {
        if (!creature.IsValid || destination.Area is not { IsValid: true } || creature.Area != destination.Area)
            throw new InvalidOperationException("Transfer did not reach its destination area.");
    }

    private async Task MoveCreatureAsync(NwPlayer player, NwCreature creature, Location destination,
        System.Action validate, bool rollback)
    {
        await NwTask.SwitchToMainThread();
        validate();
        ValidateDestination(destination);
        string playerName = player.PlayerName;
        string source = creature.Area?.ResRef ?? "<none>";
        string target = destination.Area.ResRef;
        Log.Info("Ship transfer {0}: player={1}, creature={2}, source={3}, destination={4}, position={5}, api=NWScript.JumpToLocation.",
            rollback ? "ROLLBACK MOVE" : "MOVE", playerName, creature.ObjectId, source, target, destination.Position);
        // Match Anvil's object-context pattern. JumpToLocation inserts at the TOP
        // of the creature's queue; it still needs a server frame to execute.
        await creature.WaitForObjectContext();
        validate();
        ValidateDestination(destination);
        if (rollback) NWScript.ClearAllActions(0); // Cancel any unexecuted original jump before returning.
        NWScript.JumpToLocation(destination);
        Log.Info("Ship transfer queued: player={0}, rollback={1}, immediateArea={2}.", playerName, rollback, creature.Area?.ResRef ?? "<none>");
        bool arrived = false;
        try
        {
            await ShipTransferTransaction.AwaitArrivalAsync(async () =>
            {
                await NwTask.NextFrame();
                await NwTask.SwitchToMainThread();
            }, () => destination.Area is { IsValid: true } && creature.Area == destination.Area, () =>
            {
                validate();
                ValidateDestination(destination);
            });
            arrived = true;
        }
        finally
        {
            Log.Info("Ship transfer after engine processing: player={0}, rollback={1}, destination={2}, actualArea={3}, actualPosition={4}, confirmed={5}.",
                playerName, rollback, target, creature.IsValid ? creature.Area?.ResRef ?? "<none>" : "<invalid creature>",
                creature.IsValid ? creature.Position.ToString() : "<unavailable>", arrived);
        }
    }

    private static void ValidateDestination(Location destination)
    {
        var position = destination.Position;
        if (destination.Area is not { IsValid: true } || !float.IsFinite(position.X) ||
            !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
            throw new InvalidOperationException("Transfer destination area or coordinates are unavailable/invalid.");
    }

    private void RemoveStrandedMembership(string shipName, NwPlayer player)
    {
        // A disconnected player is cleaned by OnClientLeave. If native rollback
        // fails, never retain membership/helm ownership in a deck they did not reach.
        if (player.IsValid && GetShipForPlayer(player.PlayerName) == shipName && !IsPlayerPresent(shipName, player))
            RemovePlayerAboard(shipName, player);
    }

    private void CommitTransfer(NwPlayer player, string? fromShip, string? toShip)
    {
        string name = player.PlayerName;
        if (fromShip == toShip) return;
        // Mutate both associations before invoking subscribers. A transfer cannot fail between
        // removing the old association and creating the new association.
        ShipCrewLifecycle.TransferMembership(_aboardPlayers, fromShip, toShip, name);
        StampRentalVisit(player, toShip == null ? null : _registry.GetShip(toShip));
        if (fromShip != null)
        { Notify(PlayerDetached, fromShip, name); Notify(PlayerLeft, fromShip, player); }
        if (toShip != null) Notify(PlayerBoarded, toShip, player);
    }

    public void CleanupPlayers()
    {
        Dictionary<string, NwPlayer> players = NwModule.Instance.Players.ToDictionary(p => p.PlayerName, StringComparer.Ordinal);
        foreach (var ship in _aboardPlayers.ToArray())
            foreach (string name in ship.Value.ToArray())
            {
                if (IsTransferPending(name)) continue;
                if (players.TryGetValue(name, out NwPlayer? player))
                {
                    if (!IsPlayerPresent(ship.Key, player)) RemovePlayerAboard(ship.Key, player);
                }
                else
                {
                    ship.Value.Remove(name);
                    Notify(PlayerDetached, ship.Key, name);
                    if (ship.Value.Count == 0) _aboardPlayers.Remove(ship.Key);
                }
            }
    }

    private void HandleClientLeave(ModuleEvents.OnClientLeave evt)
    {
        string? ship = GetShipForPlayer(evt.Player.PlayerName);
        if (ship != null) RemovePlayerAboard(ship, evt.Player);
    }

    // -----------------------------------------------------------------
    // Board Ship
    // -----------------------------------------------------------------

    public async Task<bool> BoardShipAsync(
        string shipName,
        NwPlayer player)
    {
        PhysicalShip? physicalShip =
            GetPhysicalShip(shipName);

        if (physicalShip == null || _registry.GetShip(shipName) is not { IsActiveWorld: true } boardingShip ||
            (_access != null && !_access.CanBoard(boardingShip, player)))
        {
            player.SendServerMessage(
                "That ship could not be found.");

            return false;
        }

        // -------------------------------------------------------------
        // Prevent duplicate boarding
        // -------------------------------------------------------------

        if (IsPlayerAboard(
                shipName,
                player.PlayerName))
        {
            player.SendServerMessage(
                $"You are already aboard the {shipName}.");

            //Log.Info(
            //$"Player {player.PlayerName} attempted " +
            //$"to board '{shipName}', but is already aboard.");

            return false;
        }

        // -------------------------------------------------------------
        // Prevent being aboard multiple ships
        // -------------------------------------------------------------

        string? existingShip =
            GetShipForPlayer(
                player.PlayerName);

        if (existingShip != null)
        {
            player.SendServerMessage(
                $"You are already aboard the {existingShip}.");

            //Log.Info(
            //$"Player {player.PlayerName} attempted " +
            //$"to board '{shipName}', but is already " +
            //$"aboard '{existingShip}'.");

            return false;
        }

        NwCreature? creature =
            player.ControlledCreature;

        if (creature == null ||
            !creature.IsValid)
        {
            Log.Warn(
                $"Cannot board '{shipName}': " +
                $"player '{player.PlayerName}' " +
                "has no valid controlled creature.");

            return false;
        }

        if (!TryGetBoardingLocation(shipName, out Location? boardingLocation) || boardingLocation == null)
        {
            player.SendServerMessage("The ship's deck is currently unavailable.");
            return false;
        }
        if (!await TryTransferPlayerAsync(player, null, shipName, boardingLocation)) return false;

        player.SendServerMessage(
            $"You board the {shipName}.");

        //Log.Info(
        //$"Player {player.PlayerName} boarded " +
        //$"the {shipName}. " +
        //$"Deck={physicalShip.DeckAreaResRef}, " +
        //$"Position=(" +
        //$"{DeckSpawnX:0.00}, " +
        //$"{DeckSpawnY:0.00}, " +
        //$"{DeckSpawnZ:0.00})");

        return true;
    }

    // -----------------------------------------------------------------
    // Leave Ship
    // -----------------------------------------------------------------

    public async Task<bool> LeaveShipAsync(
        string shipName,
        NwPlayer player)
    {
        if (GetShipForPlayer(player.PlayerName) != shipName || !IsPlayerPresent(shipName, player) ||
            _registry.GetShip(shipName) is not { IsActiveWorld: true } leavingShip)
        { player.SendServerMessage("You are not aboard an active ship with an available exit."); return false; }

        if (IsTransferPending(player.PlayerName))
        { player.SendServerMessage("Your ship transfer is already in progress."); return false; }
        if (PortDisembarkRequested == null)
        { player.SendServerMessage(ShipDisembarkRules.UnavailableMessage); return false; }
        // Port service owns destination selection; the exit cannot initiate ship-to-ship boarding.
        return await PortDisembarkRequested(leavingShip, player);
    }

    // -----------------------------------------------------------------
    // Click Handlers
    // -----------------------------------------------------------------

    private void HandlePhysicalShipClick(
        PlaceableEvents.OnLeftClick obj)
    {
        NwPlayer player =
            obj.ClickedBy;

        NwPlaceable placeable =
            obj.Placeable;

        //Log.Info(
        //$"Physical ship clicked: " +
        //$"Player={player.PlayerName}, " +
        //$"Tag={placeable.Tag}, " +
        //$"ResRef={placeable.ResRef}");

        PhysicalShip? physicalShip =
            _physicalShips.Values.FirstOrDefault(
                x =>
                    string.Equals(
                        x.PlaceableTag,
                        placeable.Tag,
                        StringComparison.Ordinal));

        if (physicalShip == null)
        {
            Log.Warn(
                $"No physical ship registered for " +
                $"placeable tag '{placeable.Tag}'.");

            return;
        }

        if (placeable.Area != player.ControlledCreature?.Area ||
            _registry.GetShip(physicalShip.ShipName) is not { IsActiveWorld: true } clickedShip ||
            !string.Equals(placeable.Area?.ResRef, clickedShip.AreaResRef, StringComparison.OrdinalIgnoreCase))
        { player.SendServerMessage("This ship representation is unavailable here."); return; }
        ObserveInteraction(BoardShipAsync(physicalShip.ShipName, player));
    }

    private void HandleShipExitClick(
        PlaceableEvents.OnLeftClick obj)
    {
        NwPlayer player =
            obj.ClickedBy;

        NwPlaceable placeable =
            obj.Placeable;

        //Log.Info(
        //$"Ship exit clicked: " +
        //$"Player={player.PlayerName}, " +
        //$"Tag={placeable.Tag}, " +
        //$"ResRef={placeable.ResRef}");

        string? aboard = GetShipForPlayer(player.PlayerName);
        if (aboard == null) { RecoverPlayerMembership(player); aboard = GetShipForPlayer(player.PlayerName); }
        PhysicalShip? physicalShip = aboard == null ? null : GetPhysicalShip(aboard);
        if (physicalShip == null && aboard == null)
        {
            // A stored deck reconnect has no active-world membership. Reuse port recovery, with unique area/tag resolution.
            PhysicalShip[] stored = _physicalShips.Values.Where(p => p.ExitPlaceableTag == placeable.Tag &&
                _registry.GetShip(p.ShipName) is { } stranded && stranded.IsStored && player.ControlledCreature?.Area == placeable.Area &&
                (string.Equals(placeable.Area?.ResRef, p.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(placeable.Area?.ResRef, p.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (stored.Length == 1 && StoredExitRequested != null)
            { ObserveInteraction(StoredExitRequested(_registry.GetShip(stored[0].ShipName)!, player), player); return; }
        }
        if (physicalShip == null || physicalShip.ExitPlaceableTag != placeable.Tag ||
            !ShipCrewLifecycle.MatchesExit(aboard, physicalShip.ShipName, player.ControlledCreature?.Area?.ResRef,
                placeable.Area?.ResRef, physicalShip.DeckAreaResRef, physicalShip.CabinAreaResRef))
        { player.SendServerMessage("This exit does not belong to the ship you are aboard."); return; }

        ObserveInteraction(LeaveShipAsync(physicalShip.ShipName, player), player, reportRejected: false);
    }
    private static async void ObserveInteraction(Task<bool> interaction, NwPlayer? exitPlayer = null, bool reportRejected = true)
    {
        bool succeeded = false, faulted = false;
        try { succeeded = await interaction; }
        catch (Exception ex) { faulted = true; Log.Error(ex, "Physical ship interaction failed."); }
        if (!succeeded && exitPlayer != null && (reportRejected || faulted))
        {
            await NwTask.SwitchToMainThread();
            if (exitPlayer.IsValid)
                exitPlayer.SendServerMessage("Disembarking failed. The exit destination or transfer is unavailable; ask an administrator to review the sailing transfer log if it persists.");
        }
    }

    public void SpawnPhysicalShip(string shipName)
    {
        TestPhysicalShip(shipName);
    }
    // -----------------------------------------------------------------
    // Test
    // -----------------------------------------------------------------

    public bool TestPhysicalShip(
        string shipName)
    {
        NwPlaceable? placeable =
            FindShipPlaceable(
                shipName);

        if (placeable == null)
        {
            return false;
        }

        //Log.Info(
        //$"Physical ship test successful: " +
        //$"Ship={shipName}, " +
        //$"Tag={placeable.Tag}, " +
        //$"ResRef={placeable.ResRef}, " +
        //$"Position=(" +
        //$"{placeable.Position.X:0.00}, " +
        //$"{placeable.Position.Y:0.00}, " +
        //$"{placeable.Position.Z:0.00})");

        return true;
    }
    public bool MovePhysicalShip(
    string shipName,
    Location location)
    {
        NwPlaceable? placeable =
            FindShipPlaceable(shipName);

        if (placeable == null)
        {
            Log.Warn(
                $"Cannot move physical ship '{shipName}': " +
                "physical placeable was not found.");

            return false;
        }

        placeable.Location =
            location;

        //Log.Info(
        //$"Physical ship '{shipName}' moved to " +
        //$"Area={location.Area?.ResRef}, " +
        //$"Position=(" +
        //$"{location.Position.X:0.00}, " +
        //$"{location.Position.Y:0.00}, " +
        //$"{location.Position.Z:0.00})");

        return true;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_module != null) _module.OnClientLeave -= HandleClientLeave;
        foreach (PhysicalShip ship in _module?.IsValid == true ? (IEnumerable<PhysicalShip>)_physicalShips.Values : [])
        {
            if (!string.IsNullOrWhiteSpace(ship.PlaceableTag))
                foreach (NwPlaceable obj in NwObject.FindObjectsWithTag<NwPlaceable>(ship.PlaceableTag)) obj.OnLeftClick -= HandlePhysicalShipClick;
            if (!string.IsNullOrWhiteSpace(ship.ExitPlaceableTag))
                foreach (NwPlaceable obj in NwObject.FindObjectsWithTag<NwPlaceable>(ship.ExitPlaceableTag)) obj.OnLeftClick -= HandleShipExitClick;
        }
        _aboardPlayers.Clear(); _representations.Clear(); _areas.Clear();
        _module = null;
    }
}
