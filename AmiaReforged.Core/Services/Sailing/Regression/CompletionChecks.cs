#if SAILING_REGRESSION
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services.Sailing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

static class CompletionChecks
{
    private static ShipState Ship(string id, ShipType type = ShipType.Player) => new()
    { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", ShipType = type,
      MaximumHull = 140, Hull = 120, DefinitionId = id, CargoCapacity = 120 };

    public static async Task Run(Action<bool, string> check)
    {
        check(ShipPersistenceOptions.ParseBackend(null) == SailingPersistenceBackend.Files,
            "New servers have durable sailing persistence without a database migration.");
        check(ShipPersistenceOptions.ParseBackend("POSTGRES") == SailingPersistenceBackend.PostgreSql &&
            ShipPersistenceOptions.ParseBackend("disabled") == SailingPersistenceBackend.Disabled,
            "Explicit backend selection never silently changes storage authority.");
        bool badOption = false;
        try { ShipPersistenceOptions.ParseBackend("mistyped"); } catch (ArgumentException) { badOption = true; }
        check(badOption, "Unknown persistence configuration fails clearly.");
        OceanContactService contacts = new();
        OceanContact pirateContact = contacts.GetContacts("ocean_002").Single(c => c.Id == "pirate_001");
        ShipState contactObserver = Ship("observer"); contactObserver.AreaResRef = "ocean_002";
        contactObserver.X = pirateContact.X; contactObserver.Y = pirateContact.Y;
        check(contacts.GetVisibleContacts(contactObserver).Contains(pirateContact),
            "The existing configured pirate contact is active and discoverable.");
        ShipState configuredPirate = OceanContactShipFactory.Create(pirateContact)!;
        check(configuredPirate.ShipType == ShipType.Pirate && configuredPirate.SpritePrefix == "brig",
            "The existing brig contact converts to the intended pirate type and sprite.");
        contacts.ReconcileRestoredShips([configuredPirate]);
        check(!contacts.GetContacts("ocean_002").Any(c => c.Id == pirateContact.Id),
            "Restart does not duplicate a configured pirate already present in the restored fleet.");
        NativeShipCastBook casts = new();
        ShipState source = Ship("caster"), first = Ship("first"), second = Ship("second");
        DateTime now = DateTime.UtcNow;
        casts.Begin("one", 6, source, first, now);
        casts.Begin("two", 6, source, second, now);
        check(!casts.TryComplete("one", 7, source, now, out _), "Unrelated spell impact does not consume the pending cast.");
        check(casts.TryComplete("one", 6, source, now, out var cast) && cast!.Target == first,
            "Native impact uses the explicit target captured before later selection changes.");
        check(!casts.TryComplete("one", 6, source, now, out _), "Separate duplicate impact objects cannot apply a second effect.");
        check(casts.TryComplete("two", 6, source, now, out cast) && cast!.Target == second,
            "Two aboard players retain independent native cast targets.");
        casts.Begin("one", 6, source, first, now);
        check(!casts.TryComplete("one", 6, source, now + NativeShipCastBook.Lifetime, out _),
            "Interrupted native casts expire without producing a free effect.");
        casts.Begin("one", 6, source, first, now);
        check(!casts.TryComplete("one", 6, Ship(source.ShipId), now, out _), "Ship replacement invalidates a pending native cast.");
        casts.Begin("one", 6, source, first, now); casts.Cancel("one");
        check(!casts.TryComplete("one", 6, source, now, out _), "Disconnect and transfer cancellation remove native cast state.");
        casts.Begin("one", 6, source, first, now); casts.Begin("one", 7, source, second, now);
        check(!casts.TryComplete("one", 6, source, now, out _) && casts.TryComplete("one", 7, source, now, out cast) && cast!.Target == second,
            "A replacement engine cast supersedes the interrupted cast rather than delivering its old target.");

        string directory = Path.Combine(Path.GetTempPath(), "amia-sailing-file-tests-" + Guid.NewGuid().ToString("N"));
        ShipPersistenceOptions options = new() { Enabled = true, Backend = SailingPersistenceBackend.Files, DataDirectory = directory };
        // This connection cannot serve a database. Successful file tests prove
        // file mode does not need or alter the application PostgreSQL store.
        SailingPersistenceContextFactory factory = SailingPersistenceContextFactory.ForOptions(
            new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1").Options);
        try
        {
            SailingFileStore files = new(options);
            using ShipStatePersistenceService persistence = new(factory, options: options, files: files);
            check(await persistence.LoadState("missing") == null && (await persistence.LoadFleet()).Count == 0,
                "First startup handles a nonexistent sailing data directory.");
            ShipState stored = Ship("stored"); stored.IsStored = true; stored.StoredPortId = "southport";
            stored.Cargo.Add(new() { ItemId = "timber", Quantity = 12 });
            Task<bool> save = persistence.TrySaveState(stored);
            stored.Cargo[0].Quantity = 99;
            check(await save, "File mode saves without a database or schema update.");
            SavedShipState loaded = (await persistence.LoadState(stored.ShipId))!;
            ShipState restored = Ship(stored.ShipId);
            check(ShipSnapshotCodec.TryRestore(restored, loaded, out _) && restored.IsStored &&
                restored.StoredPortId == "southport" && restored.Cargo.Single().Quantity == 12 && restored.Hull == 120,
                "Disk snapshot owns cargo and restores hull, storage and port coherently.");
            using ShipStatePersistenceService restart = new(factory, options: options, files: new(options));
            check((await restart.LoadState(stored.ShipId))!.SnapshotJson == loaded.SnapshotJson,
                "A new service instance restores committed data after restart.");
            List<Task<bool>> writes = [];
            for (int i = 0; i < 80; i++) { stored.X = i; writes.Add(persistence.TrySaveState(stored)); }
            check((await Task.WhenAll(writes)).All(success => success) && (await restart.LoadState(stored.ShipId))!.X == 79,
                "Overlapping actual file saves serialize and retain the newest complete snapshot.");
            stored.X = float.NaN;
            check(!await persistence.TrySaveState(stored), "Invalid serialized data reports a failed save.");
            check((await restart.LoadState(stored.ShipId))!.X == 79, "A failed save preserves the last committed disk snapshot.");
            stored.X = 80;
            check(await persistence.TrySaveState(stored), "A failed save does not permanently block subsequent saves.");
            check(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories).Any(),
                "Completed or failed file writes leave no temporary snapshots.");
            ShipState pirate = Ship("persisted-pirate", ShipType.Pirate);
            pirate.RepresentationTag = "configured_pirate"; pirate.RepresentationResRef = "configured_blueprint";
            pirate.Hull = 0;
            check(await persistence.TrySaveState(pirate), "Disabled NPC contacts are durable without being destroyed.");
            ShipState? npc = ShipSnapshotCodec.CreateRestoredNpc((await persistence.LoadState(pirate.ShipId))!);
            check(npc != null && npc.ShipType == ShipType.Pirate && npc.ShipId == pirate.ShipId && npc.IsDisabled &&
                npc.DeckAreaResRef == "deck" && npc.RepresentationTag == pirate.RepresentationTag &&
                npc.RepresentationResRef == pirate.RepresentationResRef && !npc.Underway,
                "Restart reconstructs NPC identity, type, deck, representation metadata and disabled state.");
            check(ShipSnapshotCodec.CreateRestoredNpc(loaded) == null, "Unknown player ships cannot bypass configured fleet definitions on restart.");
            check((await persistence.LoadFleet()).Count == 2, "The durable fleet catalogue includes configured ships and converted contacts.");

            using ChartDiscoveryPersistenceService charts = new(factory, options, files);
            string grid = new string('0', 255) + "1";
            check(await charts.Save(new() { AccountKey = "alpha", AreaResRef = "OCEAN_01", Cells = grid }),
                "File chart discovery persists an exact 256-cell snapshot.");
            check(await charts.Save(new() { AccountKey = "alpha", AreaResRef = "ocean_01", Cells = grid }) &&
                (await charts.Load("alpha")).Count == 1, "Case-insensitive area identities cannot create duplicate chart files.");
            check((await charts.Load("alpha")).Single().Cells == grid && (await charts.Load("beta")).Count == 0,
                "Chart disk round trips preserve fog and account separation.");
            check(!await charts.Save(new() { AccountKey = "alpha", AreaResRef = "ocean_01", Cells = "invalid" }),
                "Invalid chart data cannot overwrite committed exploration.");
            check(await persistence.FlushAsync() && await charts.FlushAsync(), "Shutdown flush drains actual ship and chart save queues.");

            string shipFile = Directory.GetFiles(Path.Combine(directory, "ships")).First(path =>
                JsonSerializer.Deserialize<SavedShipState>(File.ReadAllText(path))!.ShipId == stored.ShipId);
            File.WriteAllText(shipFile, "{invalid");
            bool corrupt = false;
            try { await restart.LoadState(stored.ShipId); } catch (JsonException) { corrupt = true; }
            check(corrupt && File.ReadAllText(shipFile) == "{invalid", "A corrupt saved ship is rejected without overwriting its file.");
            check((await restart.LoadFleet()).Single().ShipId == pirate.ShipId,
                "One corrupt file does not prevent restoring unrelated durable NPCs.");
            string chartFile = Directory.GetFiles(Path.Combine(directory, "charts"), "*.json", SearchOption.AllDirectories).Single();
            File.WriteAllText(chartFile, "{invalid");
            corrupt = false;
            try { await charts.Load("alpha"); } catch (JsonException) { corrupt = true; }
            check(corrupt, "Corrupt chart restoration fails closed rather than revealing or erasing saved fog.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        CoalescingSaveQueue<string, object> queue = new();
        TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = queue.Enqueue("pending", new(), _ => release.Task);
        Task<bool> flush = queue.FlushAsync();
        check(!flush.IsCompleted, "Shutdown flushing waits for outstanding disk/database writers.");
        release.SetResult(true);
        check(await flush, "Shutdown flush completes after the outstanding writer succeeds.");
    }
}

static class HostileBoardingChecks
{
    private sealed class World(ShipRegistryService registry, ShipEncounterService encounters, HostileBoardingStore store) : IHostileBoardingWorld
    {
        public readonly Dictionary<string, string?> Membership = new(StringComparer.Ordinal);
        public readonly Dictionary<string, BoardingPoint> Positions = new(StringComparer.Ordinal);
        public readonly Dictionary<string, BoardingDefenderState> Encounters = new(StringComparer.Ordinal);
        private readonly ShipTransferReservations _reservations = new();
        public bool Authorized = true, HasDeck = true, HasSpawn = true, HasBlueprint = true, HasRecovery = true,
            Online = true, AllowReturn = true, ExportWorks = true, FailMove, FailRollback, FailPrepare;
        public bool PartyPending;
        public bool JournalBeforeJump = true;
        public int SpawnCalls, Alive, PeakAlive, Transfers, Frames, Commits;
        public TaskCompletionSource? HoldMove;
        public Action? DuringFrame;
        public Task MainThread() => Task.CompletedTask;
        public HostileBoardingFacts Inspect(string actor, ShipState source, ShipState? target, bool transferring = false) => new(
            registry.IsCurrent(source), registry.IsCurrent(target), target != null && encounters.IsInEncounter(source, target),
            Online && Membership.GetValueOrDefault(actor) == source.ShipName && (OnShip(actor, source) || transferring),
            Authorized, PartyPending || !transferring && _reservations.Contains(actor), HasDeck, HasSpawn, HasBlueprint, HasRecovery,
            source.Underway || source.IsNavigating ? 1 : 0, target != null && (target.Underway || target.IsNavigating) ? 1 : 0);
        public BoardingPoint? Deck(ShipState ship) => HasDeck && HasSpawn ? new(ship.DeckAreaResRef, 43, 43, 0) : null;
        public BoardingPoint? Emergency() => HasRecovery ? new("safe_port", 10, 10, 0) : null;
        public bool PrepareDefenders(string actor, ShipState target, out string encounter)
        {
            encounter = Guid.NewGuid().ToString("N");
            if (FailPrepare || !HasBlueprint) return false;
            SpawnCalls++; Alive += 2; PeakAlive = Math.Max(PeakAlive, Alive);
            Encounters[encounter] = BoardingDefenderState.Fighting; return true;
        }
        public BoardingDefenderState Defenders(string encounter) => Encounters.GetValueOrDefault(encounter, BoardingDefenderState.Unavailable);
        public void CleanupDefenders(string encounter) { if (Encounters.Remove(encounter)) Alive -= 2; }
        public bool OnShip(string actor, ShipState ship) => Online && Membership.GetValueOrDefault(actor) == ship.ShipName &&
            Positions.TryGetValue(actor, out var point) && (point.Area == ship.DeckAreaResRef || point.Area == ship.CabinAreaResRef);
        public bool CanRecover(string actor, HostileBoardingVisit visit) => Online && Positions.TryGetValue(actor, out var point) &&
            (point.Area == visit.Target.Area || point.Area == visit.Origin.Area || point.Area == visit.TargetCabinArea || point.Area == visit.OriginCabinArea);
        public bool SafelyOutsideVisit(string actor, HostileBoardingVisit visit) => Online && Positions.TryGetValue(actor, out var point) &&
            point.Area == "safe_port";
        public bool AtPoint(string actor, BoardingPoint point) => Online && Positions.GetValueOrDefault(actor) == point;
        public bool Checkpoint(string actor) => Online && ExportWorks;
        public BoardingPoint? ReturnDestination(string actor, HostileBoardingVisit visit, out string? ship)
        {
            ship = null;
            var origin = registry.GetShip(visit.OriginId);
            if (AllowReturn && origin is { ShipType: ShipType.Player, IsActiveWorld: true, IsDisabled: false } && origin.Rental?.RentalId == visit.RentalId && Deck(origin) is { } point)
            { ship = origin.ShipName; return point; }
            return HasRecovery ? visit.Recovery : null;
        }
        public async Task<bool> Transfer(string actor, string? fromShip, string? toShip, BoardingPoint point, Func<bool> validate, bool recovery = false)
        {
            if (!Online) return false;
            if (recovery) fromShip = Membership.GetValueOrDefault(actor);
            using var reservation = _reservations.TryReserve([actor]);
            if (reservation == null) return false;
            Transfers++;
            var previous = Positions[actor];
            int frames = 0;
            return await ShipTransferTransaction.ExecuteAsync([
                (async () =>
                {
                    if (HoldMove != null) await HoldMove.Task;
                    if (!validate()) throw new InvalidOperationException("Boarding preflight changed");
                    JournalBeforeJump &= await store.Load(actor) != null;
                    await ShipTransferTransaction.AwaitArrivalAsync(() =>
                    {
                        Frames++; frames++;
                        if (Online) Positions[actor] = point;
                        DuringFrame?.Invoke();
                        return Task.CompletedTask;
                    }, () => !FailMove && Online && Positions[actor] == point, () =>
                    { if (!validate()) throw new InvalidOperationException("Boarding changed during native frame"); }, maximumFrames: 2);
                }, () =>
                {
                    if (FailRollback) throw new InvalidOperationException("Native rollback failed");
                    Positions[actor] = previous;
                    return Task.CompletedTask;
                }, () => { if (!validate() || !Online || Positions[actor] != point || frames == 0) throw new InvalidOperationException("Arrival unconfirmed"); })
            ], () =>
            {
                Membership[actor] = toShip;
                if (fromShip != null && registry.GetShip(fromShip) is { } source) ShipCrewLifecycle.ReleaseOwnHelm(source, actor);
                Commits++;
            });
        }
    }
    private sealed class Fleet(string directory)
    {
        public string RootPath { get; } = directory;
        public readonly ShipRegistryService Registry = new();
        public readonly ShipEncounterService Encounters = new();
        public readonly HostileBoardingOptions Options = new() { Enabled = true };
        public readonly ShipState Source = new() { ShipId = "origin", ShipName = "origin", DeckAreaResRef = "origin_deck",
            CabinAreaResRef = "origin_cabin", AreaResRef = "ocean_01", X = 20, Y = 20, HelmsmanPCKey = "captain" };
        public readonly ShipState Target = new() { ShipId = "enemy", ShipName = "enemy", DeckAreaResRef = "enemy_deck",
            CabinAreaResRef = "enemy_cabin", AreaResRef = "ocean_01", X = 24, Y = 20, ShipType = ShipType.Pirate, MaximumHull = 140, Hull = 70 };
        public HostileBoardingStore Store { get; private set; } = null!;
        public World World { get; private set; } = null!;
        public HostileBoardingEngine Engine { get; set; } = null!;
        public Fleet Initialize()
        {
            Registry.Register(Source); Registry.Register(Target); Encounters.CheckEncounters([Source, Target]);
            Store = new(new SailingFileStore(new ShipPersistenceOptions { Backend = SailingPersistenceBackend.Files, DataDirectory = directory }));
            World = new(Registry, Encounters, Store);
            Engine = new(Registry, Options, World, Store);
            Add("solo"); Add("second"); Add("third"); Add("captain");
            Source.Cargo.Add(new() { ItemId = "grain", Quantity = 12 });
            Target.Cargo.Add(new() { ItemId = "timber", Quantity = 7 });
            return this;
        }
        public void Add(string actor) { World.Membership[actor] = Source.ShipName; World.Positions[actor] = World.Deck(Source)!; }
    }
    private sealed class FailedSave(HostileBoardingStore store) : IHostileBoardingStore
    {
        public Task<HostileBoardingVisit?> Load(string actor) => store.Load(actor);
        public Task Save(HostileBoardingVisit visit) => Task.FromException(new IOException("Test journal unavailable"));
        public Task Remove(string actor) => store.Remove(actor);
    }

    public static async Task Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "amia-hostile-boarding-tests-" + Guid.NewGuid().ToString("N"));
        int number = 0;
        Fleet New() => new Fleet(Path.Combine(directory, (++number).ToString())).Initialize();
        try
        {
            var f = New();
            bool Allowed() => f.Engine.Check("solo", f.Source, f.Target).Allowed;
            check(Allowed(), "Explicit weakened hostile NPC in a current pairwise encounter is eligible.");
            check(!ShipActionRules.AllowsOrdinaryBoarding(f.Target) && ShipActionRules.AllowsOrdinaryBoarding(f.Source),
                "Physical-ship and ordinary membership recovery entry points require hostile admission for pirates, preserving friendly boarding.");
            check(!ShipAccessService.MayCommand(f.Target, "anyone") && ShipAccessService.MayCommand(f.Source, "anyone"),
                "Hostile deck membership never grants NPC helm/capture permission or alters ordinary player ship command access.");
            check(!(await f.Engine.BoardAsync("solo", f.Source, null)).Allowed && f.World.Transfers == 0 && f.World.SpawnCalls == 0,
                "No explicit target never falls back to another encounter neighbor.");
            f.Target.ShipType = ShipType.Merchant; check(!Allowed(), "Friendly merchant NPC is rejected.");
            check(ShipActionRules.AllowsOrdinaryBoarding(f.Target), "Friendly merchant physical boarding is preserved.");
            f.Target.ShipType = ShipType.Player; check(!Allowed(), "Player-owned targets cannot be forced boarded.");
            f.Target.ShipType = ShipType.Pirate; f.Target.HelmsmanPCKey = "other-player"; check(!Allowed(), "An NPC with a player helm is protected.");
            f.Target.HelmsmanPCKey = null; f.Target.RentalPoolId = "rental-pool"; check(!Allowed(), "Rental pool slots cannot be hostile targets.");
            f.Target.RentalPoolId = null; f.Target.Rental = new(); check(!Allowed(), "Active or historical rental records also protect the target.");
            f.Target.Rental = null;
            check(!f.Engine.Check("solo", f.Source, f.Source).Allowed, "Self boarding is rejected.");
            f.Target.X = 25; check(Allowed(), "Boarding range includes the configured five-unit boundary.");
            f.Target.X = 25.01f; check(!Allowed(), "Boarding outside the configured range is rejected.");
            f.Target.X = 24; f.Target.Hull = 71; check(!Allowed(), "Hull threshold uses maximum hull, rejecting above half of 140.");
            f.Target.Hull = 70; check(Allowed(), "Exactly half maximum hull is eligible.");
            f.Source.Underway = true; check(!Allowed(), "Moving attacker cannot board under stopped-only rules.");
            f.Source.Underway = false; f.Target.IsNavigating = true; check(!Allowed(), "Autonomous target movement also blocks boarding.");
            f.Options.MaximumMovementPerTick = 1; check(Allowed(), "Configured slow-movement allowance is honored.");
            f.Options.MaximumMovementPerTick = 0; f.Target.IsNavigating = false;
            f.World.HasDeck = false; check(!Allowed(), "Missing or ambiguous target deck is rejected."); f.World.HasDeck = true;
            f.World.HasSpawn = false; check(!Allowed(), "Missing target spawn waypoint is rejected."); f.World.HasSpawn = true;
            f.World.HasBlueprint = false; check(!Allowed(), "Missing verified defender assets gates the feature."); f.World.HasBlueprint = true;
            f.World.HasRecovery = false; check(!Allowed(), "A verified emergency recovery destination is mandatory."); f.World.HasRecovery = true;
            f.World.Authorized = false; check(!Allowed(), "Unauthorized passengers and expired rental access are rejected."); f.World.Authorized = true;
            f.World.PartyPending = true; check(!Allowed(), "A pending party transition blocks hostile boarding."); f.World.PartyPending = false;
            f.World.Positions["solo"] = new("unrelated", 1, 1, 0); check(!Allowed(), "Actor must physically occupy the departure ship."); f.Add("solo");
            f.Target.AreaResRef = "ocean_002"; check(!Allowed(), "Different ocean areas reject boarding."); f.Target.AreaResRef = "ocean_01";
            f.Encounters.CheckEncounters([f.Source]); check(!Allowed(), "Stale encounter adjacency cannot authorize boarding.");
            f.Encounters.CheckEncounters([f.Source, f.Target]);
            f.Registry.Remove(f.Target); check(!Allowed(), "Removed target instances reject boarding."); f.Registry.Register(f.Target);
            f.Source.IsStored = true; check(!Allowed(), "Inactive attacking ships reject boarding."); f.Source.IsStored = false;
            f.Options.Enabled = false; check(!Allowed(), "Missing operator enablement fails closed."); f.Options.Enabled = true;

            // Independent selections use the same production selection model as each NUI.
            ShipTargetSelection sailing = new(); CombatTargetSelectionService combat = new();
            ShipState other = new() { ShipName = "other", ShipId = "other", DeckAreaResRef = "other_deck", AreaResRef = "ocean_01", X = 22, Y = 20, ShipType = ShipType.Merchant };
            f.Registry.Register(other); f.Encounters.CheckEncounters([f.Source, f.Target, other]);
            var targets = f.Encounters.GetNearbyShips(f.Source).ToArray();
            sailing.Refresh("solo", f.Source, targets); combat.Selection.Refresh("solo", f.Source, targets);
            sailing.Select("solo", f.Source, Array.IndexOf(targets, f.Target), targets);
            combat.Selection.Select("solo", f.Source, Array.IndexOf(targets, other), targets);
            check(sailing.GetSelected("solo", f.Source, targets) == f.Target && combat.Selection.GetSelected("solo", f.Source, targets) == other,
                "Combat and Sailing retain independent explicit targets.");
            check(!f.Engine.Check("solo", f.Source, combat.Selection.GetSelected("solo", f.Source, targets)).Allowed &&
                f.Engine.Check("solo", f.Source, sailing.GetSelected("solo", f.Source, targets)).Allowed,
                "Hostile eligibility uses the caller's selected target without changing the other view.");

            f = New();
            var boarding = await f.Engine.BoardAsync("solo", f.Source, f.Target);
            check(boarding.Allowed && f.World.OnShip("solo", f.Target) && f.World.Frames > 0 && f.World.Commits == 1 && f.World.JournalBeforeJump,
                "Solo transfer awaits engine frames and journals recovery before committing membership.");
            check(f.World.Membership["second"] == f.Source.ShipName && f.World.Membership["captain"] == f.Source.ShipName && f.World.Transfers == 1,
                "Boarding never automatically teleports the party or remaining crew.");
            check(f.Source.HelmsmanPCKey == "captain" && !f.Source.IsNavigating && f.Source.Cargo.Single().Quantity == 12,
                "A crew boarder preserves the captain's helm, cargo and navigation.");
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && f.World.SpawnCalls == 1,
                "Duplicate boarding does not move or spawn defenders again.");
            check((await f.Engine.BoardAsync("second", f.Source, f.Target)).Allowed && f.World.SpawnCalls == 1 && f.World.PeakAlive == 2,
                "Multiple independently consenting boarders share one bounded defender encounter.");
            f.Options.MaximumBoarders = 2;
            check(!(await f.Engine.BoardAsync("third", f.Source, f.Target)).Allowed, "Configured maximum boarding party size is enforced.");
            check(!HostileBoardingRules.Loot().Allowed && f.Target.Cargo.Single().Quantity == 7 && f.Source.Cargo.Single().Quantity == 12,
                "Loot attempts cannot mutate persistent merchant/pirate cargo without a loot transaction.");
            f.Source.X = 300; f.Source.Underway = true; f.Encounters.CheckEncounters([f.Source, f.Target]);
            check(await f.Engine.ReturnAsync("solo") && f.World.OnShip("solo", f.Source), "Retreat reaches the originating ship after it sails out of encounter range.");
            check(await f.Store.Load("solo") == null && f.World.Alive == 2, "Confirmed return clears only this boarder's journal and retains other boarders' defenders.");
            foreach (string id in f.World.Encounters.Keys.ToArray()) f.World.Encounters[id] = BoardingDefenderState.Defeated;
            f.Engine.Reconcile();
            check(f.Engine.IsShipEngaged(f.Target), "Defender defeat resolves and suppresses further hostile ship AI.");
            check(await f.Engine.ReturnAsync("second") && f.World.Alive == 0, "Last boarder retreat cleans the controlled defender encounter.");
            f.Source.Underway = false; f.Source.X = 20; f.Encounters.CheckEncounters([f.Source, f.Target]);
            check(!(await f.Engine.BoardAsync("third", f.Source, f.Target)).Allowed && f.World.SpawnCalls == 1,
                "A completed deck encounter cannot respawn defenders through repeated boarding.");

            f = New(); check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Retreat scenario boards normally.");
            check(await f.Engine.ReturnAsync("solo") && !f.Engine.IsShipEngaged(f.Target) && f.World.Alive == 0,
                "Retreat before victory cleans defenders and releases the NPC to its existing AI.");
            check(!(await f.Engine.BoardAsync("second", f.Source, f.Target)).Allowed && f.World.SpawnCalls == 1,
                "Repeated retreat and reboarding cannot farm an unlimited sequence of defender waves.");

            f = New(); f.World.FailMove = true;
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && f.World.OnShip("solo", f.Source) && f.World.Commits == 0,
                "Failed arrival rolls the character back and preserves original membership.");
            check(await f.Store.Load("solo") == null && f.World.Alive == 0 && !f.Engine.HasVisit("solo"),
                "Confirmed rollback removes the unused journal and bounded defenders.");
            f.World.FailMove = false;
            check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "A failed transfer releases its reservation for retry.");

            f = New(); f.World.FailMove = true; f.World.FailRollback = true;
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && await f.Store.Load("solo") != null && f.Engine.HasVisit("solo"),
                "Unconfirmed native rollback retains durable recovery rather than forgetting the stranded character.");
            f.World.FailMove = f.World.FailRollback = false;
            check(await f.Engine.ReturnAsync("solo") && f.World.OnShip("solo", f.Source), "Recovery repairs an uncommitted membership after native rollback failure.");

            f = New(); f.World.HoldMove = new();
            Task<HostileBoardingEligibility> pending = f.Engine.BoardAsync("solo", f.Source, f.Target);
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Overlapping asynchronous boarding attempts reserve the actor before any await.");
            f.World.HoldMove.SetResult(); check((await pending).Allowed && f.World.SpawnCalls == 1, "Only one overlapping attempt creates defenders and commits.");

            f = New(); f.World.DuringFrame = () => f.Target.X = 100;
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && f.World.OnShip("solo", f.Source),
                "Range changes during the awaited native transfer trigger rollback.");

            foreach (string problem in new[] { "missing-origin", "disabled-origin", "origin-changed-type", "rental-generation", "rental-revoked", "missing-target", "target-cabin", "missing-target-in-cabin", "restart", "disconnect" })
            {
                f = New(); check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Recovery scenario begins with a confirmed boarding.");
                if (problem == "missing-origin") f.Registry.Remove(f.Source);
                if (problem == "disabled-origin") f.Source.Hull = 0;
                if (problem == "origin-changed-type") f.Source.ShipType = ShipType.Pirate;
                if (problem == "rental-generation") f.Source.Rental = new() { RentalId = "different-agreement" };
                if (problem == "rental-revoked") f.World.AllowReturn = false;
                if (problem is "missing-target" or "missing-target-in-cabin") f.Registry.Remove(f.Target);
                if (problem is "target-cabin" or "missing-target-in-cabin") f.World.Positions["solo"] = new("enemy_cabin", 10, 10, 0);
                if (problem == "disconnect")
                {
                    f.World.Online = false;
                    check(!await f.Engine.ReturnAsync("solo") && await f.Store.Load("solo") != null, "Disconnect retains the per-character return journal.");
                    f.World.Online = true;
                }
                if (problem is "restart" or "disconnect")
                {
                    f.Engine.DisposeDefenders();
                    f.Engine = new(f.Registry, f.Options, f.World, f.Store);
                    f.World.Membership["solo"] = null; // Transient membership is absent after reconnect.
                }
                check(await f.Engine.ReturnAsync("solo"), $"{problem}: recovery transaction completes.");
                bool landfall = problem is "missing-origin" or "disabled-origin" or "origin-changed-type" or "rental-generation" or "rental-revoked";
                check(f.World.Positions["solo"].Area == (landfall ? "safe_port" : f.Source.DeckAreaResRef) &&
                    f.World.Membership["solo"] == (landfall ? null : f.Source.ShipName), $"{problem}: return uses the verified origin or safe landfall without granting a new rental visit.");
                check(await f.Store.Load("solo") == null, $"{problem}: journal clears after confirmed arrival and export.");
            }
            f = New(); check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Replacement-target recovery begins with a confirmed boarding visit.");
            f.Registry.Remove(f.Target);
            ShipState replacement = new() { ShipId = f.Target.ShipId, ShipName = f.Target.ShipName,
                DeckAreaResRef = f.Target.DeckAreaResRef, CabinAreaResRef = f.Target.CabinAreaResRef,
                AreaResRef = f.Target.AreaResRef, X = f.Target.X, Y = f.Target.Y, ShipType = ShipType.Pirate,
                MaximumHull = f.Target.MaximumHull, Hull = f.Target.Hull };
            f.Registry.Register(replacement);
            check(f.Engine.RequiresRecovery("solo") && !f.Engine.IsShipEngaged(replacement),
                "Replacing an NPC with the same ID and deck invalidates the original defender session and requires recovery.");
            check(await f.Engine.ReturnAsync("solo") && f.World.OnShip("solo", f.Source) &&
                f.World.Alive == 0 && await f.Store.Load("solo") == null,
                "Replacement-target recovery returns the boarder and cleans the original defenders/journal without capturing the replacement.");
            f.Engine.Reconcile();
            check(!f.Engine.IsShipEngaged(replacement), "Old defender-session reconciliation leaves the replacement NPC independent.");

            f = New(); check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Export-failure scenario boards normally.");
            f.World.ExportWorks = false;
            check(!await f.Engine.ReturnAsync("solo") && await f.Store.Load("solo") != null, "Failed character export retains the recovery journal even after physical return.");
            f.World.ExportWorks = true; check(await f.Engine.ReturnAsync("solo"), "A later export retries recovery safely.");
            f = New(); check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Prior-journal admission test creates a confirmed visit.");
            f.Engine.DisposeDefenders(); f.Add("solo");
            f.Engine = new(f.Registry, f.Options, f.World, f.Store);
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && f.Engine.HasVisit("solo") && f.World.SpawnCalls == 1,
                "Admission discovers a prior durable journal and exposes recovery without creating another encounter.");
            check(await f.Engine.ReturnAsync("solo") && await f.Store.Load("solo") == null,
                "A character restored at the origin can resolve a previous boarding journal safely.");
            f = New(); f.World.FailPrepare = true;
            check(!(await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && f.World.Transfers == 0,
                "Defender preparation failure prevents any teleport.");
            f = New();
            var failingJournal = new HostileBoardingEngine(f.Registry, f.Options, f.World, new FailedSave(f.Store));
            bool failedSave = false;
            try { await failingJournal.BoardAsync("solo", f.Source, f.Target); } catch (IOException) { failedSave = true; }
            check(failedSave && f.World.Transfers == 0 && f.World.Alive == 0 && failingJournal.Check("solo", f.Source, f.Target).Allowed,
                "Journal write failure prevents departure, cleans defenders and releases the operation reservation.");
            f = New(); check((await f.Engine.BoardAsync("captain", f.Source, f.Target)).Allowed && f.Source.HelmsmanPCKey == null,
                "A captain who explicitly boards releases only their own helm through normal membership cleanup.");
            f = New();
            var chartAreas = new SailingAreaService(); var chartObstacles = new ShipObstacleService();
            var chartNavigation = new ShipNavigationService(chartObstacles, chartAreas);
            var chartCourse = new ChartCourseSession("captain", f.Source, new ShipRoutePlannerService(chartAreas), chartNavigation, chartObstacles);
            var chartPoint = NauticalChartGeometry.Project(60, 20, 640);
            check(chartCourse.Select(f.Source, true, chartPoint.X, chartPoint.Y, 640, out _) && chartCourse.Confirm(f.Source, true, out _),
                "Combined boarding/navigation scenario engages the actual chart course through preview and confirmation.");
            var installedChart = chartNavigation.GetRoute(f.Source);
            check(!f.Engine.Check("solo", f.Source, f.Target).Allowed && ReferenceEquals(chartNavigation.GetRoute(f.Source), installedChart),
                "A running chart autopilot blocks stopped-only hostile boarding without changing the selected course.");
            chartNavigation.CancelNavigation(f.Source);
            check(f.Engine.Check("solo", f.Source, f.Target).Allowed && !f.Source.Underway,
                "Cancelling chart navigation restores stopped-only boarding eligibility.");
            f.Options.MaximumMovementPerTick = 1;
            check(chartCourse.Select(f.Source, true, chartPoint.X, chartPoint.Y, 640, out _) && chartCourse.Confirm(f.Source, true, out _),
                "The combined scenario can explicitly re-engage its chart course under configured slow boarding rules.");
            installedChart = chartNavigation.GetRoute(f.Source);
            check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && installedChart?.IsChartCourse == true &&
                ReferenceEquals(chartNavigation.GetRoute(f.Source), installedChart) && f.Source.IsNavigating && f.Source.Underway &&
                f.Source.HelmsmanPCKey == "captain" && f.Source.Cargo.Single().Quantity == 12,
                "Hostile transfer of a crew member preserves the captain's confirmed chart course, propulsion and cargo.");

            f = New();
            ShipNavigationService sourceNavigation = new(new ShipObstacleService(), new SailingAreaService());
            ShipNavigationRoute sourceRoute = new() { ShipName = f.Source.ShipName, Waypoints = [new() { AreaResRef = "ocean_01", X = 60, Y = 20 }] };
            sourceNavigation.SetRoute(f.Source, sourceRoute); f.Options.MaximumMovementPerTick = 1;
            check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed && f.Source.IsNavigating && f.Source.Underway &&
                f.Source.DestinationX == 60 && ReferenceEquals(sourceNavigation.GetRoute(f.Source), sourceRoute) && f.Source.HelmsmanPCKey == "captain",
                "Under configured slow-movement rules, departing crew preserves the real autonomous route, propulsion and captain's helm.");
            check(await f.Store.Load("unrelated-character") == null && !await f.Engine.ReturnAsync("unrelated-character"),
                "Another character cannot consume or use a boarder's durable recovery record.");
            f = New(); check((await f.Engine.BoardAsync("solo", f.Source, f.Target)).Allowed, "Corrupt journal test begins with a valid visit.");
            string journal = Path.Combine(f.RootPath, "hostile-boarding", MaritimeIdentity.Hash("solo") + ".json");
            File.WriteAllText(journal, "invalid json");
            bool corrupt = false;
            var restarted = new HostileBoardingEngine(f.Registry, f.Options, f.World, f.Store);
            try { await restarted.ReturnAsync("solo"); } catch (JsonException) { corrupt = true; }
            check(corrupt && File.ReadAllText(journal) == "invalid json", "Corrupt recovery data fails closed and is never silently replaced or deleted.");
            Console.WriteLine("PASS: hostile boarding eligibility, transfer, defenders and durable recovery checks.");
        }
        finally
        {
            string resolved = Path.GetFullPath(directory);
            if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to delete test artifacts outside temporary storage.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
    }
}

#endif
