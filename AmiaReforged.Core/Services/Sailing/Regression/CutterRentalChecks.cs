#if SAILING_REGRESSION
using System.Reflection;
using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Sailing;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

static class CutterRentalChecks
{
    // Definitions use the configured cutter identifiers. Native resources are not loaded; test_* objects remain isolated fixtures.
    sealed class Wallet(string account) : IMaritimeWallet
    {
        public string Account => account;
        public string Character => account + "-character";
        public bool IsValid => true;
        public long Gold { get; private set; } = 1000;
        public int Calls { get; private set; }
        public void ChangeGold(int delta, string? receipt = null)
        { if (Gold + delta < 0) throw new InvalidOperationException("Insufficient funds."); Gold += delta; Calls++; }
    }
    sealed class Fleet : IDisposable
    {
        readonly Dictionary<Type, object> _services;
        public readonly ShipRegistryService Registry;
        public readonly HelmService Helm;
        public readonly PhysicalShipService Physical;
        public readonly ShipStatePersistenceService Persistence;
        public readonly ShipStorageService Storage;
        public readonly MaritimeLedgerService Ledger;
        public readonly SailingFileStore Files;
        public readonly ShipCombatService Combat;
        public readonly ShipCrewService Crew;
        public readonly ShipState[] Cutters;
        public readonly ShipState Sea;
        public Fleet(string directory)
        {
            var options = new MaritimeOptions { RentalsEnabled = true, RentalVessels = CutterRentalFleet.DefaultSlots() };
            options.Validate();
            var persistence = new ShipPersistenceOptions { DataDirectory = directory };
            var factory = SailingPersistenceContextFactory.ForOptions(new DbContextOptionsBuilder<SailingPersistenceDbContext>()
                .UseInMemoryDatabase("cutter-" + Guid.NewGuid().ToString("N")).Options);
            _services = new() { [typeof(EventService)] = new EventService([]), [typeof(DatabaseContextFactory)] = new DatabaseContextFactory(),
                [typeof(ShipPersistenceOptions)] = persistence, [typeof(SailingPersistenceContextFactory)] = factory, [typeof(MaritimeOptions)] = options };
            var resolve = StartupChecks.CreateResolver(_services);
            Helm = (HelmService)resolve(typeof(HelmService)); Registry = (ShipRegistryService)resolve(typeof(ShipRegistryService));
            Physical = (PhysicalShipService)resolve(typeof(PhysicalShipService)); Storage = (ShipStorageService)resolve(typeof(ShipStorageService));
            Persistence = (ShipStatePersistenceService)resolve(typeof(ShipStatePersistenceService)); Ledger = (MaritimeLedgerService)resolve(typeof(MaritimeLedgerService));
            Files = (SailingFileStore)resolve(typeof(SailingFileStore));
            Combat = (ShipCombatService)resolve(typeof(ShipCombatService)); Crew = (ShipCrewService)resolve(typeof(ShipCrewService));
            var port = new IslandService().GetIslands().Single(p => p.Id == "southport");
            Cutters = options.RentalVessels.Select(slot =>
            {
                var definition = CutterRentalFleet.CreateDefinition(slot, port);
                var ship = Helm.CreateShip(definition); Physical.RegisterPhysicalShip(definition); return ship;
            }).ToArray();
            var seaDefinition = ((ShipDefinition[])typeof(SailingBootstrapService).GetField("StartingFleet", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetValue(null)!).Single(s => s.ShipName == "Sea Sprite");
            Sea = Helm.CreateShip(seaDefinition);
        }
        public async Task Initialize()
        {
            var port = new IslandService().GetIslands().Single(p => p.Id == "southport");
            foreach (var ship in Registry.GetShips())
            {
                var saved = await Persistence.LoadState(ship.ShipId, ship.ShipName);
                if (saved == null) { Storage.InitializeDefaultStorage(ship, null, port); await Persistence.TrySaveInitialState(ship); }
                else if (!ShipSnapshotCodec.TryRestore(ship, saved, out _)) throw new Exception("Fixture restoration failed.");
                ship.IsInitializing = false;
            }
            await Ledger.InitializeAsync(Registry);
        }
        public Task<MaritimeResult> Reserve(ShipState ship, ShipRentalRecord lease) => Ledger.ExecuteAsync([ship],
            st => ShipRentalRules.Reserve(st, ship, lease), () => Registry.IsCurrent(ship), kind: "rental-reserve", waitForGate: true);
        public Task<MaritimeResult> Activate(ShipState ship, ShipRentalRecord lease, Wallet wallet) => Ledger.ExecuteAsync([ship],
            st => ShipRentalRules.Activate(st, lease.RentalId, wallet.Account, DateTime.UtcNow), () => Registry.IsCurrent(ship),
            wallet, "rental-checkout", lease.RentalId, waitForGate: true);
        public void Dispose() { foreach (var item in _services.Values.OfType<IDisposable>().Reverse()) item.Dispose(); }
    }
    static ShipRentalRecord Lease(ShipState ship, Wallet wallet) => new()
    { ShipId = ship.ShipId, RenterAccount = wallet.Account, PayerCharacter = wallet.Character, PortId = "southport", Fee = 100, Deposit = 200,
        DamageRate = 2, CheckoutHull = ship.Hull, Status = ShipRentalStatus.CheckoutPending };
    static bool Throws(Action action) { try { action(); return false; } catch (ArgumentException) { return true; } catch (InvalidOperationException) { return true; } catch (InvalidDataException) { return true; } }
    public static async Task Run(Action<bool, string> check)
    {
        var defaults = new MaritimeOptions();
        check(defaults.RentalVessels.Count == 2 && defaults.RentalVessels.All(s => s.VesselClass == "Cutter" && s.MaximumHull == 75 &&
            s.CargoCapacity == 60 && s.Fee == 100 && s.Deposit == 200 && s.IsConfigured && s.ShipId != "Sea Sprite"),
            "Two default cutter slots have configurable 75 hull/60 cargo/100 fee/200 deposit and the operator-supplied isolated asset mappings.");
        var duplicate = MaritimeMarketEngine.Copy(defaults.RentalVessels);
        duplicate[0].DeckAreaResRef = "test_same"; duplicate[1].DeckAreaResRef = "TEST_SAME";
        check(Throws(() => CutterRentalFleet.ValidateIsolation(duplicate)), "Case-insensitive deck collisions fail before registration or checkout.");
        duplicate[1].DeckAreaResRef = "test_other"; duplicate[1].CabinAreaResRef = "test_same";
        check(Throws(() => CutterRentalFleet.ValidateIsolation(duplicate)), "A cabin cannot alias another cutter's deck.");
        var port = new IslandService().GetIslands().Single(p => p.Id == "southport");
        check(Throws(() => CutterRentalFleet.CreateDefinition(new RentalVesselOption(), port)), "Missing deck/helm/exit/ocean mappings cannot manufacture a cutter definition.");
        var configured = new RentalVesselOption { DeckAreaResRef = "test_custom", HelmTag = "test_h", ExitTag = "test_e", PlaceableTag = "test_p",
            MaximumHull = 90, CargoCapacity = 70, Fee = 120, Deposit = 250 };
        var customDefinition = CutterRentalFleet.CreateDefinition(configured, port);
        check(customDefinition.Hull == 90 && customDefinition.CargoCapacity == 70 && customDefinition.WeaponResRef == "" &&
            customDefinition.CabinAreaResRef == "" && !customDefinition.AllowsWeapons, "Configuration can change hull/cargo/terms without requiring a fabricated cabin or arming the cutter.");
        check(Throws(() => CutterRentalFleet.CreateDefinition(configured, new IslandLocation { Id = "invalid" })), "Invalid rental port mappings fail definition creation.");
        string directory = Path.Combine(Path.GetTempPath(), "amia-cutters-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var fleet = new Fleet(directory); await fleet.Initialize();
            var a = fleet.Cutters[0]; var b = fleet.Cutters[1];
            check(fleet.Ledger.Ready && a.IsStored && b.IsStored && a.StoredPortId == "southport" && !a.IsActiveWorld && !b.IsActiveWorld,
                "Actual cutter definitions initialize behind persistence gates as stored Southport vessels.");
            check(a.ShipId != b.ShipId && a.DeckAreaResRef != b.DeckAreaResRef && a.MaximumHull == 75 && b.CargoCapacity == 60 &&
                a.WeaponResRef == "" && !a.AllowsWeapons && !b.AllowsWeapons, "Every slot has its own canonical unarmed ship and interior.");
            check(fleet.Sea.ShipId == "Sea Sprite" && fleet.Sea.RentalPoolId == null && fleet.Sea.Rental == null &&
                fleet.Sea.WeaponResRef == "ship_cannon" && fleet.Sea.MaximumHull == 100 && fleet.Sea.DeckAreaResRef == "sea_sprite_d2",
                "Sea Sprite stays an ordinary configured ship with its original equipment and interior.");
            check(!ShipRentalService.Available(fleet.Sea, "southport") && ShipAccessService.MayCommand(fleet.Sea, "anyone") &&
                !ShipAccessService.MayBoard(a, "anyone"), "Communal access remains separate from protected cutter availability.");
            var duplicateShip = new ShipState { ShipId = "test-imposter", ShipName = "Test imposter", DeckAreaResRef = a.DeckAreaResRef,
                AreaResRef = "ocean_01", DefinitionId = "Cutter", AllowsWeapons = false };
            check(Throws(() => fleet.Registry.Register(duplicateShip)) && !fleet.Registry.IsCurrent(duplicateShip),
                "Canonical registry rejects another ship sharing a cutter deck without replacing either instance.");
            check(!new ShipRentalService(fleet.Registry, null!, fleet.Physical, fleet.Ledger, defaults, new IslandService(), fleet.Persistence)
                .ResourcesAvailable(fleet.Sea), "An ordinary ship cannot pass cutter resource validation.");
            check(!new ShipRentalService(fleet.Registry, null!, fleet.Physical, fleet.Ledger,
                    new MaritimeOptions { RentalVessels = [new RentalVesselOption()] }, new IslandService(), fleet.Persistence)
                .ResourcesAvailable(a), "Unmapped cutter resources fail availability before any native area lookup or player charge.");
            var wa = new Wallet("account-A"); var wb = new Wallet("account-B"); var la = Lease(a, wa); var lb = Lease(b, wb);
            var reserved = await Task.WhenAll(fleet.Reserve(a, la), fleet.Reserve(b, lb));
            check(reserved.All(r => r.Success) && a.Rental!.RentalId != b.Rental!.RentalId && wa.Calls == 0 && wb.Calls == 0,
                "Two concurrent requests atomically reserve different vessels/decks without charging either player.");
            check(!(await fleet.Reserve(a, Lease(a, wb))).Success, "A competing checkout cannot double-assign the first cutter.");
            var leases = fleet.Ledger.Snapshot().Rentals.Where(r => MaritimeMarketEngine.IsOpen(r.Status)).ToArray();
            check(leases.Length == 2 && leases.Select(r => r.DeckAreaResRef).Distinct().Count() == 2 && leases.All(r => r.VesselDefinitionId == "Cutter"),
                "Durable reservations record and validate isolated cutter deck assignments.");
            var bad = fleet.Ledger.Snapshot(); bad.Rentals[1].DeckAreaResRef = bad.Rentals[0].DeckAreaResRef;
            check(Throws(() => MaritimeMarketEngine.Validate(bad)), "Corrupt persistence cannot restore two open leases sharing a deck.");
            var activated = await Task.WhenAll(fleet.Activate(a, la, wa), fleet.Activate(b, lb, wb));
            check(activated.All(r => r.Success) && wa.Gold == 700 && wb.Gold == 700 && wa.Calls == 1 && wb.Calls == 1,
                "Concurrent independent activations charge each account exactly once.");
            fleet.Storage.RetrieveShip(a, "southport"); fleet.Storage.RetrieveShip(b, "southport");
            check(!(await fleet.Activate(a, la, wa)).Success && wa.Gold == 700, "Duplicate activation never debits a second rental fee.");
            a.HelmsmanPCKey = "captain-A"; b.HelmsmanPCKey = "captain-B"; a.Underway = b.Underway = true;
            a.X = 91; b.X = 94; a.Y = b.Y = 100;
            var playerHit = await fleet.Combat.TryAttack(a, b, null!);
            a.WeaponResRef = "ship_heavy_cannon";
            var npcHit = await fleet.Combat.TryNpcAttack(a, b);
            check(playerHit.HasNoWeapons && npcHit.HasNoWeapons && !playerHit.Success && b.Hull == 75 &&
                fleet.Combat.GetCooldownRemaining(a) == TimeSpan.Zero && !fleet.Combat.TryEquipWeapon(a, "ship_cannon") && fleet.Combat.GetWeapon(a).Damage == 0,
                "Both domain attack entry points and equipment reject an unarmed cutter, even with forged weapon data.");
            var malformedCutter = new ShipState { ShipName = "Malformed Cutter", DeckAreaResRef = "test_malformed", AreaResRef = "ocean_01",
                DefinitionId = "Cutter", WeaponResRef = "ship_cannon" };
            check((await fleet.Combat.TryAttack(malformedCutter, b, null!)).HasNoWeapons && !fleet.Combat.TryEquipWeapon(malformedCutter, "ship_cannon"),
                "The Cutter definition itself also blocks weapon attacks/equipment on malformed external state.");
            var encounter = new ShipEncounterService(); encounter.CheckEncounters([a, b, fleet.Sea]);
            check(encounter.IsInEncounter(a, b) && !encounter.IsInEncounter(a, fleet.Sea), "Active cutters create independent encounters; stored Sea Sprite stays excluded.");
            var combat = new ShipTargetSelection(); var sailing = new ShipTargetSelection();
            combat.Refresh("captain-A", a, [b]); combat.Select("captain-A", a, 0, [b]);
            sailing.Refresh("captain-B", b, [a]); sailing.Select("captain-B", b, 0, [a]);
            check(combat.GetSelected("captain-A", a, [b]) == b && sailing.GetSelected("captain-B", b, [a]) == a,
                "Different cutters retain independent per-player Sailing/Combat targets.");
            var nativeCasts = new NativeShipCastBook(); var castAt = DateTime.UtcNow;
            nativeCasts.Begin("caster-A", 6, a, b, castAt);
            check(nativeCasts.TryComplete("caster-A", 6, a, castAt, out var nativeCast) && nativeCast!.Target == b,
                "Unarmed cutters retain the existing explicit native ship-magic cast bookkeeping; native spell payment/VFX still require live testing.");
            var aboard = new Dictionary<string, HashSet<string>>();
            ShipCrewLifecycle.TransferMembership(aboard, null, a.ShipName, "captain-A"); ShipCrewLifecycle.TransferMembership(aboard, null, b.ShipName, "captain-B");
            ShipCrewLifecycle.TransferMembership(aboard, null, a.ShipName, "passenger");
            var setRole = typeof(ShipCrewService).GetMethod("SetRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
            setRole.Invoke(fleet.Crew, [a.ShipName, "captain-A", ShipCrewRole.Captain]); setRole.Invoke(fleet.Crew, [b.ShipName, "captain-B", ShipCrewRole.Captain]);
            setRole.Invoke(fleet.Crew, [a.ShipName, "passenger", ShipCrewRole.Passenger]);
            ShipCrewLifecycle.TransferMembership(aboard, a.ShipName, null, "passenger"); fleet.Crew.RemoveMember(a.ShipName, "passenger");
            check(!ShipCrewLifecycle.ReleaseOwnHelm(a, "passenger") && a.HelmsmanPCKey == "captain-A" && b.HelmsmanPCKey == "captain-B" &&
                a.Underway && b.Underway && aboard[b.ShipName].Contains("captain-B") && fleet.Crew.GetCaptain(a.ShipName)?.PlayerName == "captain-A" &&
                fleet.Crew.GetCaptain(b.ShipName)?.PlayerName == "captain-B", "Passenger cleanup preserves both cutters' independent crew, helms and navigation.");
            check(ShipAccessService.MayRecoverRentalVisit(a, wa.Account, la.RentalId) && !ShipAccessService.MayRecoverRentalVisit(a, wb.Account, la.RentalId) &&
                !ShipAccessService.MayRecoverRentalVisit(a, wa.Account, Guid.NewGuid().ToString("N")),
                "Reconnect requires current account authorization and the same voyage generation, never a stale cutter visit.");
            var trade = await fleet.Ledger.ExecuteAsync([a], st => MaritimeMarketEngine.PortTrade(st, st.Vessels[a.ShipId], a.CargoCapacity,
                "southport", "grain", 60, true, wa.Gold, DateTime.UtcNow), () => true, wa);
            check(trade.Success && a.Cargo.Single().Quantity == 60 && b.Cargo.Count == 0 && !ReferenceEquals(a.Cargo, b.Cargo),
                "Trading fills only the purchasing cutter's 60-unit cargo hold.");
            check(!(await fleet.Ledger.ExecuteAsync([a], st => MaritimeMarketEngine.PortTrade(st, st.Vessels[a.ShipId], a.CargoCapacity,
                "southport", "grain", 1, true, wa.Gold, DateTime.UtcNow), () => true, wa)).Success, "The cutter cargo capacity is enforced by the economic domain.");
            await fleet.Persistence.TrySaveState(a); await fleet.Persistence.TrySaveState(b); await fleet.Persistence.TrySaveState(fleet.Sea);
            using (var reboot = new Fleet(directory))
            {
                await reboot.Initialize();
                check(reboot.Ledger.Ready && reboot.Cutters.All(s => !s.IsStored && s.Rental?.Status == ShipRentalStatus.Active && !s.AllowsWeapons && s.WeaponResRef == "") &&
                    reboot.Cutters[0].Cargo.Single().Quantity == 60 && reboot.Cutters[1].Cargo.Count == 0 && reboot.Cutters.All(s => s.HelmsmanPCKey == null && !s.Underway),
                    "Restart restores two isolated active rentals/cargo without replaying payments, arming cutters or restoring stale helm owners.");
                check(reboot.Sea.RentalPoolId == null && reboot.Sea.WeaponResRef == "ship_cannon" && wa.Calls == 2 && wb.Calls == 1,
                    "Cutter restoration leaves ordinary Sea Sprite and each payer's charge history unchanged.");
            }
            var snapshot = ShipSnapshotCodec.Capture(a); var payload = JsonSerializer.Deserialize<ShipSnapshotPayload>(snapshot.SnapshotJson!)!;
            payload.DeckAreaResRef = b.DeckAreaResRef; snapshot.SnapshotJson = JsonSerializer.Serialize(payload);
            check(!ShipSnapshotCodec.TryRestore(a, snapshot, out _) && a.DeckAreaResRef != b.DeckAreaResRef, "A changed persisted deck cannot mutate a configured cutter.");
            fleet.Storage.StoreShip(a, "southport"); fleet.Storage.StoreShip(b, "southport");
            check(!ShipRentalService.CanReturn(a, wa.Account, "southport", false) && !ShipRentalService.CanReturn(b, wb.Account, "southport", true) &&
                !ShipRentalService.CanReturn(b, wa.Account, "southport", false), "Cargo, occupants and a different renter independently prevent deck release.");
            b.Hull = 65;
            var returned = await fleet.Ledger.ExecuteAsync([b], st => ShipRentalRules.Return(st, b, wb.Account, "southport", false, DateTime.UtcNow),
                () => true, wb, "rental-return", lb.RentalId);
            check(returned.Success && wb.Gold == 880 && b.Rental == null && ShipRentalService.Available(b, "southport") && b.Hull == 65 && a.Rental?.RentalId == la.RentalId,
                "Return settles 20 gp damage/refunds 180, releases only the second vessel/deck and preserves the first rental and both hull/cargo states.");
            var replacementLease = Lease(b, wb); check((await fleet.Reserve(b, replacementLease)).Success && (await fleet.Activate(b, replacementLease, wb)).Success,
                "A successfully returned cutter can start a new independent voyage agreement.");
            check(!ShipAccessService.MayRecoverRentalVisit(b, wb.Account, lb.RentalId) && ShipAccessService.MayRecoverRentalVisit(b, wb.Account, replacementLease.RentalId),
                "Deck reuse cannot associate an offline passenger's old visit with the new voyage.");
            await fleet.Ledger.ExecuteAsync([b], st => ShipRentalRules.Cancel(st, replacementLease.RentalId, true, DateTime.UtcNow), () => true, wb);
            var failedLease = Lease(b, wb); await fleet.Reserve(b, failedLease); long goldBefore = wb.Gold;
            bool moved = false, committed = false;
            bool transfer = await ShipTransferTransaction.ExecuteAsync([
                (() => { moved = true; return Task.CompletedTask; }, () => { moved = false; return Task.CompletedTask; },
                 () => { if (!moved) throw new InvalidOperationException("Not arrived."); })], () => committed = true,
                prepareCommit: async () => { var result = await fleet.Activate(b, failedLease, wb); if (!result.Success) throw new Exception(result.Message); throw new InvalidOperationException("Injected late transfer failure."); },
                abortCommit: async () => { await fleet.Ledger.ExecuteAsync([b], st => ShipRentalRules.Cancel(st, failedLease.RentalId, true, DateTime.UtcNow), () => true, wb); });
            check(!transfer && !moved && !committed && b.IsStored && b.Rental == null && wb.Gold == goldBefore && a.Rental?.RentalId == la.RentalId && a.Cargo.Single().Quantity == 60,
                "Failed checkout compensates the exact payment and movement, releases only its own reservation and preserves the other cutter.");
            await fleet.Persistence.TrySaveState(a); await fleet.Persistence.TrySaveState(b);
            using (var final = new Fleet(directory))
            { await final.Initialize(); check(final.Ledger.Ready && final.Cutters[0].Rental?.RentalId == la.RentalId && final.Cutters[1].Rental == null &&
                final.Cutters[1].IsStored && ShipRentalService.Available(final.Cutters[1], "southport"), "Restart retains the first rental and the released second slot after failed checkout."); }
            var list = DockyardShipList.Create(fleet.Registry, fleet.Storage, "southport", s => ShipAccessService.MayCommand(s, wa.Account));
            var activeRow = DockyardRentalAgreement.Create(a.Rental!, a, ShipAccessService.MayCommand(a, wa.Account), false);
            check(list.Rows.Any(r => r.ShipId == "Sea Sprite" && r.Vessel == "Sloop / Communal") && list.Rows.All(r => r.ShipId != b.ShipId && r.ShipId != a.ShipId) &&
                activeRow.CanRetrieve && activeRow.Name.Contains("Cutter"), "Working dockyard keeps communal ships, existing rental retrieval and free rental offers in independent sections.");
            var returnLease = Lease(b, wb); await fleet.Reserve(b, returnLease); await fleet.Activate(b, returnLease, wb);
            long beforeReturn = wb.Gold; int writes = 0;
            fleet.Ledger.SaveOverride = async st => { if (++writes == 2) throw new IOException("Injected settlement checkpoint failure."); await fleet.Files.SaveEconomy(st); };
            var failedReturn = await fleet.Ledger.ExecuteAsync([b], st => ShipRentalRules.Return(st, b, wb.Account, "southport", false, DateTime.UtcNow),
                () => true, wb, "rental-return", returnLease.RentalId);
            check(!failedReturn.Success && wb.Gold == beforeReturn && b.Rental?.RentalId == returnLease.RentalId && !ShipRentalService.Available(b, "southport") &&
                a.Rental?.RentalId == la.RentalId, "Failed return settlement compensates its refund and keeps the vessel/deck reserved without affecting the other rental.");
            fleet.Ledger.SaveOverride = null;
            await fleet.Ledger.ExecuteAsync([b], st => ShipRentalRules.Cancel(st, returnLease.RentalId, true, DateTime.UtcNow), () => true, wb);
            // A saved proof-of-concept agreement is retained for settlement, never reenrolled for a new voyage.
            var legacyWallet = new Wallet("legacy-account");
            fleet.Sea.RentalPoolId = "southport"; var legacy = Lease(fleet.Sea, legacyWallet);
            await fleet.Ledger.ExecuteAsync([fleet.Sea], st => { st.Rentals.Add(legacy); return 0; }, () => true);
            var legacyActive = await fleet.Ledger.ExecuteAsync([fleet.Sea], st => ShipRentalRules.Activate(st, legacy.RentalId, legacyWallet.Account, DateTime.UtcNow), () => true, legacyWallet);
            check(legacyActive.Success && fleet.Sea.RentalPoolId == null && fleet.Sea.Rental?.Status == ShipRentalStatus.Active && !ShipRentalService.Available(fleet.Sea, "southport"),
                "An existing Sea Sprite agreement keeps its renter/deposit obligation but cannot generate another rental.");
            await fleet.Ledger.ExecuteAsync([fleet.Sea], st => ShipRentalRules.Return(st, fleet.Sea, legacyWallet.Account, "southport", false, DateTime.UtcNow), () => true, legacyWallet);
            check(fleet.Sea.Rental == null && fleet.Sea.RentalPoolId == null && ShipAccessService.MayBoard(fleet.Sea, "anyone"),
                "Settling the legacy agreement restores ordinary communal Sea Sprite access without relocating or deleting ship data.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
#endif
