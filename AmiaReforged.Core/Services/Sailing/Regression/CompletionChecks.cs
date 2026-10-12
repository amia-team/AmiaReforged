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

#endif
