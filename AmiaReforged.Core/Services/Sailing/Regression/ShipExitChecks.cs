#if SAILING_REGRESSION
using System.Reflection;
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Sailing;
using Anvil.Services;

static class ShipExitChecks
{
    // Models the engine boundary only. No module assets, native movement or client windows are claimed verified.
    sealed class Engine(string area)
    {
        public string Area = area;
        public int Jumps;
        string? _queued;
        TaskCompletionSource _frame = new();
        public void Advance()
        {
            if (_queued != null) { Area = _queued; _queued = null; }
            var previous = _frame; _frame = new(); previous.SetResult();
        }
        public async Task Move(string destination, Action validate)
        {
            Jumps++; _queued = destination;
            await ShipTransferTransaction.AwaitArrivalAsync(() => _frame.Task, () => Area == destination, validate);
        }
        public void Require(string destination) { if (Area != destination) throw new InvalidOperationException("Not arrived."); }
    }
    public static async Task Run(Action<bool, string> check)
    {
        var islands = new IslandService(); var areas = new SailingAreaService();
        var port = islands.GetIslands().Single(p => p.Id == "southport");
        var services = new Dictionary<Type, object> { [typeof(EventService)] = new EventService([]),
            [typeof(DatabaseContextFactory)] = new DatabaseContextFactory(),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled } };
        try
        {
            var resolve = StartupChecks.CreateResolver(services);
            var helm = (HelmService)resolve(typeof(HelmService));
            var physical = (PhysicalShipService)resolve(typeof(PhysicalShipService));
            var ports = (ShipPortService)resolve(typeof(ShipPortService));
            var storage = (ShipStorageService)resolve(typeof(ShipStorageService));
            var crew = (ShipCrewService)resolve(typeof(ShipCrewService));
            var definitions = ((ShipDefinition[])typeof(SailingBootstrapService).GetField("StartingFleet", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetValue(null)!).Where(d => d.ShipName == "Sea Sprite")
                .Concat(CutterRentalFleet.DefaultSlots().Select(s => CutterRentalFleet.CreateDefinition(s, port))).ToArray();
            var ships = definitions.Select(definition => { var ship = helm.CreateShip(definition); physical.RegisterPhysicalShip(definition); return ship; }).ToArray();
            var setRole = typeof(ShipCrewService).GetMethod("SetRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
            foreach (var ship in ships)
            {
                check(storage.RetrieveShip(ship, "southport"), "The existing storage path activates each test ship before individual exit.");
                ship.HelmsmanPCKey = "captain"; ship.Underway = ship.IsNavigating = true;
                ship.Cargo.Add(new() { ItemId = "grain", Quantity = 5 });
                if (CutterRentalFleet.IsCutter(ship)) ship.Rental = new() { ShipId = ship.ShipId, Status = ShipRentalStatus.Active,
                    RenterAccount = "renter", Fee = 100, Deposit = 200, PortId = "southport" };
                ShipLandfall destination = ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea)!;
                check(destination is { PortId: "southport", AreaResRef: "cordor_west", X: 75, Y: 45, Z: 0 } &&
                    destination.AreaResRef != ship.AreaResRef && destination.X != ship.X && destination.Y != ship.Y,
                    "Sea Sprite and both cutters resolve Southport's configured landfall, never the physical ocean coordinates.");
                check(ShipDisembarkRules.DestinationAvailable(destination, 1, true, areas.ContainsArea) &&
                    !ShipDisembarkRules.DestinationAvailable(destination, 0, false, areas.ContainsArea) &&
                    !ShipDisembarkRules.DestinationAvailable(destination, 2, true, areas.ContainsArea) &&
                    !ShipDisembarkRules.DestinationAvailable(destination, 1, false, areas.ContainsArea),
                    "Missing/ambiguous area and non-walkable landing reject the same native preflight predicate.");
                check(!ShipDisembarkRules.DestinationAvailable(destination with { AreaResRef = "ocean_002" }, 1, true, areas.ContainsArea),
                    "Any registered ocean is forbidden as an individual exit destination.");
                var aboard = new Dictionary<string, HashSet<string>> { [ship.ShipName] = ["captain", "passenger", "other-passenger"] };
                setRole.Invoke(crew, [ship.ShipName, "captain", ShipCrewRole.Captain]);
                setRole.Invoke(crew, [ship.ShipName, "passenger", ShipCrewRole.Passenger]);
                setRole.Invoke(crew, [ship.ShipName, "other-passenger", ShipCrewRole.Crew]);
                var combat = new ShipTargetSelection(); var sailing = new ShipTargetSelection();
                var target = ships.First(s => s != ship);
                combat.Refresh("captain", ship, [target]); combat.Select("captain", ship, 0, [target]);
                combat.Refresh("passenger", ship, [target]); combat.Select("passenger", ship, 0, [target]);
                sailing.Refresh("captain", ship, [target]); sailing.Select("captain", ship, 0, [target]);
                sailing.Refresh("passenger", ship, [target]); sailing.Select("passenger", ship, 0, [target]);
                var rental = ship.Rental; string? storedPort = ship.StoredPortId; var cargo = ship.Cargo.Single();
                Engine engine = new(ship.DeckAreaResRef); int commits = 0;
                void Validate() { if (!ShipDisembarkRules.StillEligible(ship, destination, islands.GetIslands(), areas.ContainsArea)) throw new InvalidOperationException("Port lost."); }
                void Commit(string name)
                {
                    ShipCrewLifecycle.TransferMembership(aboard, ship.ShipName, null, name);
                    ShipCrewLifecycle.ReleaseOwnHelm(ship, name); crew.RemoveMember(ship.ShipName, name);
                    combat.ClearForSource(name, ship.ShipName); sailing.ClearForSource(name, ship.ShipName); commits++;
                }
                var reservations = new ShipTransferReservations();
                using (var reserved = reservations.TryReserve(["passenger"]))
                {
                    Task<bool> transfer = ShipTransferTransaction.ExecuteAsync([
                        (() => engine.Move(destination.AreaResRef, Validate), () => engine.Move(ship.DeckAreaResRef, () => { }),
                         () => { Validate(); engine.Require(destination.AreaResRef); })], () => Commit("passenger"));
                    check(!transfer.IsCompleted && engine.Area == ship.DeckAreaResRef && commits == 0 && aboard[ship.ShipName].Contains("passenger"),
                        "Individual disembarking awaits an engine frame before any membership, helm or UI commit.");
                    check(reservations.TryReserve(["passenger"]) == null && engine.Jumps == 1,
                        "Duplicate exit clicks cannot reserve a player or enqueue an overlapping move.");
                    engine.Advance();
                    check(await transfer && engine.Area == "cordor_west" && commits == 1 &&
                        aboard[ship.ShipName].SetEquals(["captain", "other-passenger"]),
                        "Confirmed passenger arrival at Southport removes only that passenger.");
                }
                check(ship.HelmsmanPCKey == "captain" && ship.Underway && ship.IsNavigating &&
                    crew.GetCaptain(ship.ShipName)?.PlayerName == "captain" && crew.GetRole(ship.ShipName, "other-passenger") == ShipCrewRole.Crew,
                    "Passenger exit preserves captain ownership, route state and the remaining passenger/crew.");
                check(combat.GetSelected("captain", ship, [target]) == target && sailing.GetSelected("captain", ship, [target]) == target &&
                    combat.GetSelected("passenger", ship, [target]) == null && sailing.GetSelected("passenger", ship, [target]) == null,
                    "Only the departing player's Sailing and Combat selections are cleared.");
                check(!ship.IsStored && ship.StoredPortId == storedPort && ReferenceEquals(ship.Rental, rental) &&
                    ship.Cargo.Single() == cargo && cargo.Quantity == 5 && ship.IsActiveWorld,
                    "Individual exit never stores a ship, settles a rental or changes cargo.");
                var failedEngine = new Engine(ship.DeckAreaResRef);
                Task<bool> failed = ShipTransferTransaction.ExecuteAsync([
                    (() => failedEngine.Move(destination.AreaResRef, Validate), () => failedEngine.Move(ship.DeckAreaResRef, () => { }),
                     () => { Validate(); failedEngine.Require(destination.AreaResRef); })], () => Commit("other-passenger"));
                ship.X = 400;
                failedEngine.Advance();
                check(!failed.IsCompleted && aboard[ship.ShipName].Contains("other-passenger") && ship.HelmsmanPCKey == "captain" && commits == 1,
                    "A ship leaving port range during the engine transition triggers rollback without detaching crew or helm.");
                failedEngine.Advance();
                check(!await failed && failedEngine.Area == ship.DeckAreaResRef && commits == 1 && ship.Rental == rental && !ship.IsStored,
                    "Failed exit confirms rollback to the original deck and preserves memberships/rental/storage.");
                ship.X = 90;
                var captainEngine = new Engine(ship.DeckAreaResRef);
                Task<bool> captain = ShipTransferTransaction.ExecuteAsync([
                    (() => captainEngine.Move(destination.AreaResRef, Validate), () => captainEngine.Move(ship.DeckAreaResRef, () => { }),
                     () => { Validate(); captainEngine.Require(destination.AreaResRef); })], () => Commit("captain"));
                captainEngine.Advance();
                check(await captain && ship.HelmsmanPCKey == null && aboard[ship.ShipName].SetEquals(["other-passenger"]) &&
                    ship.Underway && ship.IsNavigating && ship.Rental == rental && !ship.IsStored,
                    "Captain exit releases only their own helm under existing policy, retaining navigation and remaining crew.");
            }
            VerifyRangeAndSelection(ships[0], islands, areas, check);
            await VerifyBoardingSeparation(ships, physical, islands, areas, check);
            var eventField = typeof(PhysicalShipService).GetField("PortDisembarkRequested", BindingFlags.Instance | BindingFlags.NonPublic)!;
            check(((Delegate?)eventField.GetValue(physical))?.GetInvocationList().Length == 1 &&
                ((Delegate)eventField.GetValue(physical)!).Method.Name == nameof(ShipPortService.DisembarkAsync),
                "The native-free service graph wires one port handler without a circular constructor dependency.");
            ports.Dispose(); ports.Dispose();
            check(eventField.GetValue(physical) == null, "Port exit subscription cleanup is safe and idempotent.");
        }
        finally { foreach (var item in services.Values.OfType<IDisposable>().Reverse()) item.Dispose(); }
    }

    static void VerifyRangeAndSelection(ShipState ship, IslandService islands, SailingAreaService areas, Action<bool, string> check)
    {
        var port = islands.GetIslands().Single(p => p.Id == "southport");
        ship.X = 100; ship.Y = 100;
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea)?.PortId == "southport", "Docking radius includes its exact boundary.");
        ship.X = 100.01f;
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea) == null, "Outside every docking radius, exit remains aboard.");
        ship.X = 90; ship.AreaResRef = "ocean_002";
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea) == null, "Matching coordinates in another ocean cannot reach Southport.");
        ship.AreaResRef = "ocean_01"; ship.X = float.NaN;
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea) == null, "Non-finite ship positions cannot disembark.");
        ship.X = 90; ship.IsDocking = true;
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea) == null, "A full-crew docking transition excludes individual exits.");
        ship.IsDocking = false; ship.IsStored = true;
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea) == null && ShipDisembarkRules.InDockRange(ship, port),
            "Stored-deck recovery is distinct from active membership exits and can separately validate actual dock range.");
        ship.IsStored = false; ship.Hull = 0;
        check(ShipDisembarkRules.Resolve(ship, islands.GetIslands(), areas.ContainsArea)?.PortId == "southport", "A disabled ship at port does not trap its passengers.");
        ship.Hull = ship.MaximumHull;
        // These overlapping ports/coordinates are test-only fixtures, not claimed world assets.
        IslandLocation Overlap(string id, float anchor) => new() { Id = id, Name = id, OceanArea = "ocean_01", OceanX = anchor,
            OceanY = 100, DockRadius = 10, LandingArea = "cordor_west", LandingX = 75, LandingY = 45 };
        var alpha = Overlap("alpha", 90); var zeta = Overlap("zeta", 90);
        check(ShipDisembarkRules.Resolve(ship, [zeta, alpha], areas.ContainsArea)?.PortId == "alpha" &&
            ShipDisembarkRules.Resolve(ship, [alpha, zeta], areas.ContainsArea)?.PortId == "alpha",
            "Equal-distance overlapping radii select ordinal port ID independently of enumeration order.");
        var nearer = Overlap("zeta-nearer", 91); var farther = Overlap("alpha-farther", 95);
        check(ShipDisembarkRules.Resolve(ship, [farther, nearer], areas.ContainsArea)?.PortId == "zeta-nearer", "Distance takes priority over port ID.");
        alpha.LandingArea = "ocean_005";
        check(ShipDisembarkRules.Resolve(ship, [alpha], areas.ContainsArea) == null, "A misconfigured landfall in any ocean is rejected.");
        alpha.LandingArea = "cordor_west"; alpha.LandingX = float.PositiveInfinity;
        check(ShipDisembarkRules.Resolve(ship, [alpha], areas.ContainsArea) == null, "Invalid landfall coordinates have no fallback ocean exit.");
        alpha.LandingX = 75;
        var pinned = ShipDisembarkRules.Resolve(ship, [alpha], areas.ContainsArea)!; alpha.LandingX = 76;
        check(!ShipDisembarkRules.StillEligible(ship, pinned, [alpha], areas.ContainsArea), "Landfall changes cannot silently retarget an in-flight exit.");
    }

    static async Task VerifyBoardingSeparation(ShipState[] ships, PhysicalShipService physical, IslandService islands,
        SailingAreaService areas, Action<bool, string> check)
    {
        var source = ships[0]; var target = ships[2]; var unrelated = ships[1];
        source.X = target.X = 200; source.Y = target.Y = 200; target.HelmsmanPCKey = "target-captain";
        var book = new BoardingRequestBook();
        var request = new ShipBoardingRequest { RequestingShip = source, TargetShip = target, RequestingPlayerName = "passenger",
            TargetPlayerName = "target-captain", RequestedAt = DateTime.UtcNow };
        check(book.Add(request, DateTime.UtcNow) && ShipDisembarkRules.Resolve(source, islands.GetIslands(), areas.ContainsArea) == null &&
            book.ForTarget("other-captain") == null && book.Requests.Count == 1,
            "A pending boarding request does not grant an ocean exit or consent to another captain.");
        check(!ShipDisembarkRules.DestinationAvailable(new("southport", "Southport", target.DeckAreaResRef, 75, 45, 0),
            1, true, areas.ContainsArea, isShipInterior: true),
            "A port misconfigured to another ship's deck cannot bypass boarding consent through individual exit.");
        var accepted = book.ForTarget("target-captain")!; book.Remove(accepted);
        string deck = physical.GetPhysicalShip(accepted.TargetShip.ShipName)!.DeckAreaResRef;
        check(accepted.TargetShip == target && deck == "sp_cutter_02" && deck != physical.GetPhysicalShip(unrelated.ShipName)!.DeckAreaResRef,
            "Existing request identity resolves the explicit second cutter deck, never another encountered deck.");
        var aboard = new Dictionary<string, HashSet<string>> { [source.ShipName] = ["passenger"] };
        var engine = new Engine(source.DeckAreaResRef);
        Task<bool> transfer = ShipTransferTransaction.ExecuteAsync([
            (() => engine.Move(deck, () => { }), () => engine.Move(source.DeckAreaResRef, () => { }), () => engine.Require(deck))],
            () => ShipCrewLifecycle.TransferMembership(aboard, source.ShipName, target.ShipName, "passenger"));
        engine.Advance();
        check(await transfer && engine.Area == target.DeckAreaResRef && aboard[target.ShipName].Contains("passenger") && !aboard.ContainsKey(source.ShipName),
            "The separate accepted-boarding transaction commits only after arriving at the explicit target deck.");
        target.X = 90; target.Y = 100;
        check(ShipDisembarkRules.Resolve(target, islands.GetIslands(), areas.ContainsArea)?.AreaResRef == "cordor_west",
            "A boarded passenger uses the same landfall rules after the target vessel reaches port.");
    }
}
#endif
