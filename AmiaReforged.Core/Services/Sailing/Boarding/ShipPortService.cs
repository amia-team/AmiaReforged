using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Port transitions publish storage and membership only after confirmed native arrivals.</summary>
[ServiceBinding(typeof(ShipPortService))]
public sealed class ShipPortService : IDisposable
{
    private readonly ShipRegistryService registry;
    private readonly PhysicalShipService physical;
    private readonly ShipStorageService storage;
    private readonly IslandService islands;
    private readonly ShipNavigationService navigation;
    private readonly ShipBoardingService boarding;
    private readonly ShipStatePersistenceService persistence;
    private readonly ShipAccessService? access;
    private readonly SailingAreaService sailingAreas;
    public ShipPortService(ShipRegistryService registry, PhysicalShipService physical,
        ShipStorageService storage, IslandService islands, ShipNavigationService navigation,
        ShipBoardingService boarding, ShipStatePersistenceService persistence, SailingAreaService sailingAreas, ShipAccessService? access = null)
    {
        this.registry = registry; this.physical = physical; this.storage = storage; this.islands = islands;
        this.navigation = navigation; this.boarding = boarding; this.persistence = persistence; this.access = access;
        this.sailingAreas = sailingAreas;
        physical.StoredExitRequested += RecoverStoredExitAsync;
        physical.PortDisembarkRequested += DisembarkAsync;
    }
    public void Dispose()
    {
        physical.StoredExitRequested -= RecoverStoredExitAsync;
        physical.PortDisembarkRequested -= DisembarkAsync;
    }
    public bool HasRentalOccupants(ShipState ship) => physical.HasDeckOccupants(ship);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task<bool> DisembarkAsync(ShipState ship, NwPlayer player) => DisembarkPartyAsync(ship, [player]);

    public bool TryPlanDisembark(ShipState ship, out ShipLandfall? landfall)
    {
        landfall = ShipDisembarkRules.Resolve(ship, islands.GetIslands(), sailingAreas.ContainsArea);
        return landfall != null && TryGetLandfallLocation(ship, landfall, out _);
    }

    public async Task<bool> DisembarkPartyAsync(ShipState ship, IReadOnlyList<NwPlayer> players,
        ShipLandfall? capturedLandfall = null, Func<bool>? validateParty = null, Func<NwPlayer, bool>? validateDeparture = null)
    {
        await NwTask.SwitchToMainThread();
        if (players.Count == 0) return false;
        NwPlayer player = players[0];
        if (!registry.IsCurrent(ship) || players.Any(p => !p.IsValid || !physical.IsPlayerPresent(ship.ShipName, p) ||
            physical.GetShipForPlayer(p.PlayerName) != ship.ShipName || physical.IsTransferPending(p.PlayerName)))
        {
            if (player.IsValid) player.SendServerMessage("You are no longer aboard this ship, or a transfer is already in progress.");
            return false;
        }
        ShipLandfall? landfall = capturedLandfall ?? ShipDisembarkRules.Resolve(ship, islands.GetIslands(), sailingAreas.ContainsArea);
        if (landfall == null)
        { player.SendServerMessage(ShipDisembarkRules.UnavailableMessage); return false; }
        if (!TryGetLandfallLocation(ship, landfall, out Location? destination))
        { player.SendServerMessage($"The configured landfall at {landfall.PortName} is unavailable. You remain aboard."); return false; }
        bool Valid() => registry.IsCurrent(ship) &&
            validateParty?.Invoke() != false &&
            ShipDisembarkRules.StillEligible(ship, landfall, islands.GetIslands(), sailingAreas.ContainsArea) &&
            destination!.Area.IsValid && destination.IsWalkable && !IsShipInterior(landfall.AreaResRef);
        // Selected-player transfer: no IsDocking reservation, storage, navigation or rental mutation.
        // The shared transfer owns the player reservation and commits detach callbacks only after arrival.
        bool transferred = await physical.TryTransferPlayersAsync(players, ship.ShipName, null, destination!, Valid,
            validateDeparture: validateDeparture);
        await NwTask.SwitchToMainThread();
        foreach (var participant in players.Where(p => p.IsValid))
            participant.SendServerMessage(transferred ? $"You disembark from the {ship.ShipName} at {landfall.PortName}." :
                "Disembarking failed. Return to your original position was attempted; the ship or destination changed.");
        return transferred;
    }

    private bool TryGetLandfallLocation(ShipState ship, ShipLandfall landfall, out Location? destination)
    {
        destination = null;
        NwArea[] matches = NwModule.Instance.Areas.Where(a => a.IsValid &&
            string.Equals(a.ResRef, landfall.AreaResRef, StringComparison.OrdinalIgnoreCase)).ToArray();
        Location? location = matches.Length == 1 ?
            Location.Create(matches[0], new System.Numerics.Vector3(landfall.X, landfall.Y, landfall.Z), 0) : null;
        bool walkable = location?.IsWalkable == true;
        bool shipInterior = IsShipInterior(landfall.AreaResRef);
        if (!ShipDisembarkRules.DestinationAvailable(landfall, matches.Length, walkable, sailingAreas.ContainsArea, shipInterior))
        {
            Log.Warn("Ship landfall unavailable: ship={0}, port={1}, area={2}, found={3}, position=({4},{5},{6}), walkable={7}, shipInterior={8}.",
                ship.ShipId, landfall.PortId, landfall.AreaResRef, matches.Length, landfall.X, landfall.Y, landfall.Z, walkable, shipInterior);
            return false;
        }
        destination = location;
        return true;
    }

    private bool IsShipInterior(string areaResRef) => physical.GetPhysicalShips().Any(p =>
        string.Equals(p.DeckAreaResRef, areaResRef, StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(p.CabinAreaResRef) && string.Equals(p.CabinAreaResRef, areaResRef, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> RecoverStoredExitAsync(ShipState ship, NwPlayer player)
    {
        await NwTask.SwitchToMainThread();
        // A click is not the startup orphan-recovery exception. Even an untracked stored-deck occupant
        // may exit only at this ship's actual configured storage port within its docking radius.
        IslandLocation? port = islands.GetIslands().SingleOrDefault(p => p.Id == ship.StoredPortId);
        if (!ship.IsStored || port == null || !ShipDisembarkRules.InDockRange(ship, port) ||
            !ShipDisembarkRules.ValidLandfall(port, sailingAreas.ContainsArea))
        { if (player.IsValid) player.SendServerMessage(ShipDisembarkRules.UnavailableMessage); return false; }
        var landfall = new ShipLandfall(port.Id, port.Name, port.LandingArea, port.LandingX, port.LandingY, port.LandingZ);
        if (!TryGetLandfallLocation(ship, landfall, out _)) return false;
        return await RecoverStoredPlayerAsync(ship, player);
    }

    public async Task<bool> DockAsync(ShipState ship, NwPlayer player)
    {
        if ((access != null && !access.CanCommand(ship, player)) || !registry.IsCurrent(ship) || !ship.IsActiveWorld || ship.IsDisabled || ship.IsEconomyBusy ||
            ship.HelmsmanPCKey != player.PlayerName || !physical.IsPlayerPresent(ship.ShipName, player)) return false;
        IslandLocation? island = islands.GetNearestIsland(ship);
        if (island == null || MathF.Sqrt(MathF.Pow(ship.X - island.OceanX, 2) + MathF.Pow(ship.Y - island.OceanY, 2)) > island.DockRadius ||
            string.IsNullOrWhiteSpace(island.ShipyardTag)) return false;
        NwArea? landing = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == island.LandingArea);
        if (landing == null || !landing.IsValid ||
            !NwObject.FindObjectsWithTag<NwPlaceable>(island.ShipyardTag).Any(p => p.IsValid &&
                string.Equals(p.Area?.ResRef, island.LandingArea, StringComparison.OrdinalIgnoreCase))) return false;
        Location destination = Location.Create(landing, new System.Numerics.Vector3(island.LandingX, island.LandingY, island.LandingZ), 0);
        bool underway = ship.Underway, navigating = ship.IsNavigating;
        string? course = ship.DestinationAreaResRef, storedPort = ship.StoredPortId;
        bool transferred = false;
        ship.IsDocking = true; // Excludes encounters/navigation and a second port operation while awaiting frames.
        try
        {
            transferred = await physical.TryDisembarkAllAsync(ship.ShipName, destination,
                () =>
                {
                    if (!storage.StoreShip(ship, island.Id)) throw new InvalidOperationException("Ship storage commit failed.");
                },
                () => registry.IsCurrent(ship) && ship.IsDocking && !ship.IsStored && !ship.IsDisabled &&
                    ship.HelmsmanPCKey == player.PlayerName && ship.AreaResRef == island.OceanArea &&
                    MathF.Sqrt(MathF.Pow(ship.X - island.OceanX, 2) + MathF.Pow(ship.Y - island.OceanY, 2)) <= island.DockRadius);
            if (!transferred) return false;
            navigation.CancelNavigation(ship);
            boarding.CancelShip(ship);
            ship.HelmsmanPCKey = null;
            physical.Synchronize(ship);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Docking {0} failed for {1}.", transferred ? "post-transfer refresh" : "transition", ship.ShipName);
            if (!transferred) return false;
        }
        finally
        {
            if (!transferred)
            {
                ship.IsStored = false; ship.StoredPortId = storedPort;
                ship.Underway = underway; ship.IsNavigating = navigating; ship.DestinationAreaResRef = course;
            }
            ship.IsDocking = false;
        }
        await SaveTransitionAsync(ship);
        if (player.IsValid) player.SendServerMessage($"You make landfall at {island.Name}. The {ship.ShipName} is stored here.");
        return true;
    }

    public async Task<bool> RetrieveAsync(ShipState ship, string portId, NwPlayer player, bool rentalCheckout = false,
        Func<Task>? prepareCommit = null, Func<Task>? abortCommit = null, IReadOnlyList<NwPlayer>? party = null,
        Func<bool>? validateParty = null, Func<NwPlayer, bool>? validateDeparture = null)
    {
        IslandLocation? port = islands.GetIslands().FirstOrDefault(i => i.Id == portId);
        if (rentalCheckout && (prepareCommit == null || abortCommit == null || ship.Rental?.Status != ShipRentalStatus.CheckoutPending)) return false;
        if (validateParty?.Invoke() == false || (party != null && (party.Count == 0 || party[0] != player)) ||
            (access != null && !access.CanRetrieve(ship, player, rentalCheckout)) || !registry.IsCurrent(ship) || !storage.IsStoredAtPort(ship, portId) || port == null || ship.IsInitializing || ship.IsDocking || ship.IsEconomyBusy ||
            !player.IsValid || player.ControlledCreature is not { IsValid: true } creature ||
            !string.Equals(creature.Area?.ResRef, port.LandingArea, StringComparison.OrdinalIgnoreCase) ||
            physical.GetShipForPlayer(player.PlayerName) != null || physical.IsTransferPending(player.PlayerName) ||
            !physical.HasPhysicalShip(ship.ShipName)) return false;
        if (!physical.TryGetBoardingLocation(ship.ShipName, out Location? spawn) || spawn == null) return false;
        bool transferred = false;
        ship.IsDocking = true;
        try
        {
            // Keep the original stored record authoritative until the engine confirms arrival.
            transferred = await physical.TryTransferPlayersAsync(party ?? [player], null, ship.ShipName, spawn,
                validateAction: () => validateParty?.Invoke() != false && registry.IsCurrent(ship) && ship.IsDocking && storage.IsStoredAtPort(ship, portId),
                beforeCommit: () =>
                {
                    if (rentalCheckout && (ship.Rental?.Status != ShipRentalStatus.Active ||
                        !ShipAccessService.Owns(ship, MaritimeIdentity.Account(player))))
                        throw new InvalidOperationException("Rental payment was not activated before retrieval commit.");
                    if (!storage.RetrieveShip(ship, portId)) throw new InvalidOperationException("Ship retrieval commit failed.");
                    ship.IsDocking = false; // PlayerBoarded subscribers must see an active, retrieved ship.
                }, allowStoredTarget: true, prepareCommit: prepareCommit, abortCommit: abortCommit, validateDeparture: validateDeparture);
            if (!transferred) return false;
            try { physical.Synchronize(ship); }
            catch (Exception ex) { Log.Warn(ex, "Retrieved {0}; representation refresh failed.", ship.ShipName); }
        }
        finally
        {
            if (!transferred) { ship.IsStored = true; ship.StoredPortId = portId; }
            ship.IsDocking = false;
        }
        await SaveTransitionAsync(ship);
        // Rental-pool ships use account authorization; ships outside the pool remain communal.
        if (player.IsValid) player.SendServerMessage($"The {ship.ShipName} has been retrieved from {portId}.");
        return true;
    }

    private async Task SaveTransitionAsync(ShipState ship)
    {
        if (persistence.IsEnabled && !await persistence.TrySaveState(ship))
            Log.Error("Ship transfer committed but persistence failed: ship={0}. Retry a checkpoint before restart.", ship.ShipId);
        await NwTask.SwitchToMainThread();
    }

    public async Task<bool> RecoverStoredPlayerAsync(ShipState ship, NwPlayer player)
    {
        if (!registry.IsCurrent(ship) || ship.IsInitializing || ship.IsDocking || !player.IsValid ||
            physical.GetShipForPlayer(player.PlayerName) != null || physical.IsTransferPending(player.PlayerName) ||
            player.ControlledCreature is not { IsValid: true, Area: { } area }) return false;
        bool orphanCutter = CutterRentalFleet.IsCutter(ship) && !ship.IsStored && !ShipAccessService.MayRecoverRentalVisit(ship,
            MaritimeIdentity.Account(player), player.LoginCreature?.GetObjectVariable<LocalVariableString>("SAILING_RENTAL_VISIT").Value);
        if (!ship.IsStored && !orphanCutter) return false;
        PhysicalShip? mapping = physical.GetPhysicalShip(ship.ShipName);
        if (mapping == null || !(string.Equals(area.ResRef, mapping.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(area.ResRef, mapping.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))) return false;
        string? recoveryPort = orphanCutter ? ship.Rental?.PortId ?? ship.RentalPoolId : ship.StoredPortId;
        IslandLocation? port = islands.GetIslands().FirstOrDefault(i => i.Id == recoveryPort);
        NwArea? landing = port == null ? null : NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == port.LandingArea && a.IsValid);
        if (port == null || landing == null) return false;
        Location destination = Location.Create(landing, new System.Numerics.Vector3(port.LandingX, port.LandingY, port.LandingZ), 0);
        if (!await physical.TryTransferPlayerAsync(player, null, null, destination,
                () => registry.IsCurrent(ship) && (orphanCutter || storage.IsStoredAtPort(ship, port.Id)) && !ship.IsDocking)) return false;
        if (player.IsValid) player.SendServerMessage(orphanCutter ?
            $"Your previous cutter agreement no longer grants access to this deck. You return to {port.Name}; the current voyage is unchanged." :
            $"The {ship.ShipName} was docked while you were away. You arrive at {port.Name}.");
        return true;
    }
}
