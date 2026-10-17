using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Fixed voyage agreements on isolated cutter slots. Legacy agreements may finish, but ordinary ships cannot start new rentals.</summary>
[ServiceBinding(typeof(ShipRentalService))]
public sealed class ShipRentalService(ShipRegistryService registry, ShipPortService ports, PhysicalShipService physical,
    MaritimeLedgerService ledger, MaritimeOptions options, IslandService islands, ShipStatePersistenceService persistence)
{
    private readonly HashSet<string> _checkouts = new(StringComparer.Ordinal);
    public bool Enabled => options.RentalsEnabled && ledger.Ready && !ledger.HasRecovery;
    public IReadOnlyList<RentalVesselOption> Definitions => options.RentalVessels;
    public IReadOnlyList<ShipRentalRecord> ForAccount(string account) => ledger.Snapshot().Rentals
        .Where(r => r.RenterAccount == account && MaritimeMarketEngine.IsOpen(r.Status)).ToArray();
    public bool ResourcesAvailable(ShipState ship) => ResourceFailures(ship).Count == 0;

    public IReadOnlyList<string> ResourceFailures(ShipState ship)
    {
        var slot = options.RentalVessels.SingleOrDefault(v => v.ShipId == ship.ShipId);
        if (!registry.IsCurrent(ship) || !CutterRentalFleet.IsCutter(ship) || ship.AllowsWeapons || slot is not { IsConfigured: true })
            return ["canonical unarmed cutter or complete rental mapping unavailable"];
        var mapping = physical.GetPhysicalShip(ship.ShipName);
        if (mapping == null) return ["physical ship mapping not registered"];
        if (mapping.DeckAreaResRef != slot.DeckAreaResRef || mapping.CabinAreaResRef != slot.CabinAreaResRef ||
            mapping.HelmPlaceableTag != slot.HelmTag || mapping.ExitPlaceableTag != slot.ExitTag || mapping.PlaceableTag != slot.PlaceableTag)
            return ["registered physical mapping differs from configured cutter slot"];
        var areas = NwModule.Instance.Areas.Where(a => a.IsValid).ToArray();
        var decks = areas.Where(a => a.ResRef.Equals(mapping.DeckAreaResRef, StringComparison.OrdinalIgnoreCase)).ToArray();
        var cabins = string.IsNullOrWhiteSpace(mapping.CabinAreaResRef) ? [] :
            areas.Where(a => a.ResRef.Equals(mapping.CabinAreaResRef, StringComparison.OrdinalIgnoreCase)).ToArray();
        NwArea? deck = decks.Length == 1 ? decks[0] : null;
        NwArea? cabin = cabins.Length == 1 ? cabins[0] : null;
        var representations = physical.FindShipPlaceables(ship.ShipName).Where(p => p.IsValid).ToArray();
        var helms = NwObject.FindObjectsWithTag<NwPlaceable>(mapping.HelmPlaceableTag!).Where(p => p.IsValid).ToArray();
        var exits = NwObject.FindObjectsWithTag<NwPlaceable>(mapping.ExitPlaceableTag).Where(p => p.IsValid).ToArray();
        var spawns = NwObject.FindObjectsWithTag<NwWaypoint>("SAILING_DECK_SPAWN").Where(w => w.IsValid && deck != null && w.Area == deck).ToArray();
        Location? location = spawns.Length == 1 ? spawns[0].Location : null;
        string[] interiors = new[] { mapping.DeckAreaResRef, mapping.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        var snapshot = new CutterResourceSnapshot
        {
            DeckCount = decks.Length, CabinCount = cabins.Length,
            RepresentationCount = representations.Length,
            RepresentationInShipArea = representations.Length == 1 && representations[0].Area?.ResRef == ship.AreaResRef,
            HelmCount = helms.Length, HelmInInterior = helms.Length == 1 &&
                (deck != null && helms[0].Area == deck || cabin != null && helms[0].Area == cabin),
            ExitCount = exits.Length, DeckExitCount = deck == null ? 0 : exits.Count(p => p.Area == deck),
            CabinExitCount = cabin == null ? 0 : exits.Count(p => p.Area == cabin),
            DeckSpawnCount = spawns.Length, SpawnLocationValid = location is { Area: { IsValid: true } } && location.Area == deck &&
                float.IsFinite(location.Position.X) && float.IsFinite(location.Position.Y) && float.IsFinite(location.Position.Z),
            UniqueInteriors = !physical.GetPhysicalShips().Any(p => p.ShipName != ship.ShipName &&
                new[] { p.DeckAreaResRef, p.CabinAreaResRef }.Intersect(interiors, StringComparer.OrdinalIgnoreCase).Any())
        };
        return snapshot.Failures(slot, ship.AreaResRef);
    }

    public void LogResourceReadiness()
    {
        var log = NLog.LogManager.GetCurrentClassLogger();
        if (options.RentalsEnabled && !Enabled)
            log.Error("Rental checkout disabled: maritime persistence ready={0}, recovery required={1}, failure={2}.",
                ledger.Ready, ledger.HasRecovery, ledger.Failure ?? "none");
        foreach (var slot in Definitions)
        {
            try
            {
                var ship = registry.GetShip(slot.ShipId);
                var failures = ship == null ? new[] { "ship slot not registered; complete its rental mapping" } : ResourceFailures(ship);
                if (failures.Count != 0) log.Error("Rental cutter {0} unavailable: {1}.", slot.ShipId, string.Join("; ", failures));
                else log.Info("Rental cutter {0} resources validated: deck={1}, helm={2}, exit={3}, ocean tag={4}; checkout enabled={5}, stored port={6}.",
                    slot.ShipId, slot.DeckAreaResRef, slot.HelmTag, slot.ExitTag, slot.PlaceableTag, Enabled, ship!.StoredPortId);
            }
            catch (Exception ex) { log.Error(ex, "Rental cutter {0} resource validation failed; checkout remains unavailable.", slot.ShipId); }
        }
    }
    // Compatibility with existing callers: registration/restoration now owns fleet membership.
    // Opening a shipyard must never enroll a communal ship or mutate the persistent fleet.
    public Task EnrollAsync() => Task.CompletedTask;
    public static bool Available(ShipState ship, string port) => CutterRentalFleet.IsCutter(ship) && !ship.AllowsWeapons &&
        ship.ShipType == ShipType.Player && ship.RentalPoolId == port &&
        ship.Rental == null && ship.IsStored && ship.StoredPortId == port && !ship.IsInitializing && !ship.IsDocking &&
        !ship.IsDisabled && ship.Cargo.All(c => c.Quantity == 0);
    private bool AtPort(NwPlayer player, string portId) => player.IsValid && player.LoginCreature == player.ControlledCreature &&
        player.ControlledCreature is { IsValid: true, Area: { } area } &&
        islands.GetIslands().Any(i => i.Id == portId && string.Equals(i.LandingArea, area.ResRef, StringComparison.OrdinalIgnoreCase));
    public async Task<MaritimeResult> CheckoutAsync(NwPlayer player, string shipId, string port,
        Func<bool>? validateTravel = null, Func<NwPlayer, bool>? validateDeparture = null)
    {
        if (!Enabled) return MaritimeResult.Fail("Ship rentals are disabled or awaiting persistence/resource validation.");
        var definition = options.RentalVessels.SingleOrDefault(v => v.ShipId == shipId && v.PortId == port);
        var ship = registry.GetShip(shipId);
        if (validateTravel?.Invoke() == false || definition == null || ship == null || !AtPort(player, port) || !Available(ship, port) || !ResourcesAvailable(ship) || physical.HasDeckOccupants(ship) ||
            physical.GetShipForPlayer(player.PlayerName) != null || physical.IsTransferPending(player.PlayerName))
            return MaritimeResult.Fail("This vessel is unavailable. Its existing deck and ocean representation must be present.");
        var wallet = new MaritimeWallet(player);
        if (wallet.Gold < (long)definition.Fee + definition.Deposit) return MaritimeResult.Fail("Insufficient gold for the fee and refundable deposit.");
        if (!_checkouts.Add(shipId)) return MaritimeResult.Fail("This vessel is already being checked out.");
        var record = new ShipRentalRecord { ShipId = shipId, RenterAccount = wallet.Account, PayerCharacter = wallet.Character,
            PortId = port, Status = ShipRentalStatus.CheckoutPending, Fee = definition.Fee, Deposit = definition.Deposit,
            DamageRate = definition.DamageRate, CheckoutHull = ship.Hull };
        bool charged = false;
        try
        {
            var reserve = await ledger.ExecuteAsync([ship], state => ShipRentalRules.Reserve(state, ship, record),
                () => validateTravel?.Invoke() != false && registry.IsCurrent(ship) && AtPort(player, port) && Available(ship, port) && ResourcesAvailable(ship) && !physical.HasDeckOccupants(ship),
                kind: "rental-reserve", waitForGate: true);
            if (!reserve.Success) return reserve;
            bool retrieved = await ports.RetrieveAsync(ship, port, player, rentalCheckout: true,
                prepareCommit: async () =>
                {
                    var payment = await ledger.ExecuteAsync([ship], state =>
                        ShipRentalRules.Activate(state, record.RentalId, wallet.Account, DateTime.UtcNow), () => registry.IsCurrent(ship) && wallet.IsValid && ship.IsDocking && ship.IsStored &&
                        ship.Rental?.RentalId == record.RentalId, wallet, "rental-checkout", record.RentalId, waitForGate: true);
                    if (!payment.Success) throw new InvalidOperationException(payment.Message);
                    charged = true;
                }, abortCommit: async () =>
                {
                    var undo = await CancelAsync(ship, record, wallet, charged);
                    if (!undo.Success)
                    {
                        if (charged && ledger.Ready && !ledger.HasRecovery)
                            await ledger.ExecuteAsync([ship], state => ShipRentalRules.RequireRecovery(state, record.RentalId,
                                checked(record.Fee + record.Deposit), "Checkout rollback could not refund the original payer."),
                                () => registry.IsCurrent(ship) && ship.IsStored, kind: "rental-recovery", waitForGate: true);
                        throw new InvalidOperationException(undo.Message);
                    }
                    charged = false;
                }, validateParty: validateTravel, validateDeparture: validateDeparture);
            if (!retrieved)
            {
                // Invalid preflight can return before the transaction starts.
                if (ship.Rental?.RentalId == record.RentalId && ship.Rental.Status == ShipRentalStatus.CheckoutPending)
                    await CancelAsync(ship, record, wallet, false);
                return MaritimeResult.Fail("Checkout transfer failed. The ship remains stored; any unresolved payment is frozen for administrator recovery.");
            }
            return MaritimeResult.Ok($"Voyage rental started: {ship.ShipName}. Fee {record.Fee} gp; deposit {record.Deposit} gp. Return at Southport after docking and unloading cargo.");
        }
        finally { _checkouts.Remove(shipId); }
    }
    private Task<MaritimeResult> CancelAsync(ShipState ship, ShipRentalRecord record, IMaritimeWallet wallet, bool refund) =>
        ledger.ExecuteAsync([ship], state =>
            ShipRentalRules.Cancel(state, record.RentalId, refund, DateTime.UtcNow), () => registry.IsCurrent(ship) && ship.IsStored && (!refund || wallet.IsValid),
            refund ? wallet : null, "rental-cancel", record.RentalId, waitForGate: true);
    public static (int Damage, int Refund) Settlement(ShipRentalRecord lease, int hull)
    {
        int damage = (int)Math.Min(lease.Deposit, Math.Max(0L, (long)lease.CheckoutHull - hull) * lease.DamageRate);
        return (damage, lease.Deposit - damage);
    }
    public static bool CanReturn(ShipState ship, string account, string port, bool hasAboard) =>
        ShipAccessService.Owns(ship, account) && ship.Rental!.PortId == port && ship.IsStored && ship.StoredPortId == port &&
        !ship.IsInitializing && !ship.IsDocking && !hasAboard && ship.Cargo.All(c => c.Quantity == 0);
    public async Task<MaritimeResult> ReturnAsync(NwPlayer player, string shipId, string port)
    {
        var ship = registry.GetShip(shipId);
        if (ship == null || !AtPort(player, port)) return MaritimeResult.Fail("Visit the rental port to return this vessel.");
        var wallet = new MaritimeWallet(player);
        bool Valid() => registry.IsCurrent(ship) && AtPort(player, port) && CanReturn(ship, wallet.Account, port,
            physical.HasDeckOccupants(ship));
        if (!Valid()) return MaritimeResult.Fail("Only the renter can return this rental. Dock at Southport and sell or remove all cargo first; disembarking does not end a rental.");
        string id = ship.Rental!.RentalId;
        var settlement = Settlement(ship.Rental, ship.Hull);
        var result = await ledger.ExecuteAsync([ship], state =>
            ShipRentalRules.Return(state, ship, wallet.Account, port, physical.HasDeckOccupants(ship), DateTime.UtcNow), Valid, wallet, "rental-return", id);
        if (result.Success)
        {
            await persistence.TrySaveState(ship); await NwTask.SwitchToMainThread();
            return MaritimeResult.Ok($"Rental returned. Damage settlement {settlement.Damage} gp; deposit refund {settlement.Refund} gp. Hull and cargo were not reset.");
        }
        return result;
    }
}
