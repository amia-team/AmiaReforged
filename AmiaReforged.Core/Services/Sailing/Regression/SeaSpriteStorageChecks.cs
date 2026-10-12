#if SAILING_REGRESSION
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Sailing;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

static class SeaSpriteStorageChecks
{
    // Use the actual bootstrap definitions and constructor graph, not a second
    // copy of the fleet configuration. Native teleport/visibility is tested live.
    private static readonly ShipDefinition[] Definitions = (ShipDefinition[])typeof(SailingBootstrapService)
        .GetField("StartingFleet", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    private static ShipDefinition SeaSprite => Definitions.Single(d => d.ShipName == "Sea Sprite");

    private sealed class Fleet : IDisposable
    {
        private readonly Dictionary<Type, object> _services;
        public readonly HelmService Helm;
        public readonly ShipStatePersistenceService Persistence;
        public readonly ShipStorageService Storage;
        public readonly ShipNavigationService Navigation;
        public readonly ShipRegistryService Registry;
        public readonly IslandService Islands;
        public IslandLocation Southport => Islands.GetIslands().Single(i => i.Id == SeaSprite.InitialStoredPortId);
        public Fleet(ShipPersistenceOptions options, SailingPersistenceContextFactory factory)
        {
            _services = new()
            {
                [typeof(EventService)] = new EventService([]),
                [typeof(DatabaseContextFactory)] = new DatabaseContextFactory(),
                [typeof(ShipPersistenceOptions)] = options,
                [typeof(SailingPersistenceContextFactory)] = factory
            };
            Func<Type, object> resolve = StartupChecks.CreateResolver(_services);
            Helm = (HelmService)resolve(typeof(HelmService));
            Persistence = (ShipStatePersistenceService)resolve(typeof(ShipStatePersistenceService));
            Storage = (ShipStorageService)resolve(typeof(ShipStorageService));
            Navigation = (ShipNavigationService)resolve(typeof(ShipNavigationService));
            Registry = (ShipRegistryService)resolve(typeof(ShipRegistryService));
            Islands = (IslandService)resolve(typeof(IslandService));
        }
        public void Dispose()
        {
            foreach (IDisposable service in _services.Values.OfType<IDisposable>().Reverse()) service.Dispose();
        }
    }

    private static string ShipFile(string directory) => Path.Combine(directory, "ships",
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SeaSprite.ShipName))) + ".json");

    public static async Task Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "amia-sea-sprite-tests-" + Guid.NewGuid().ToString("N"));
        SailingPersistenceContextFactory filesFactory = SailingPersistenceContextFactory.ForOptions(
            new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1").Options);
        ShipPersistenceOptions Options(string subdirectory) => new()
        { Backend = SailingPersistenceBackend.Files, Enabled = true, DataDirectory = Path.Combine(directory, subdirectory) };
        try
        {
            ShipPersistenceOptions options = Options("fresh");
            using (Fleet fleet = new(options, filesFactory))
            {
                check(SeaSprite.InitialStoredPortId == "southport" && fleet.Southport.ShipyardTag == "southport_shipyard" &&
                    fleet.Southport.LandingArea == "cordor_west", "Bootstrap storage and actual Southport retrieval configuration agree.");
                check(Definitions.Where(d => d != SeaSprite).All(d => d.InitialStoredPortId == null),
                    "Sea Sprite is the only starting ship with a stored default.");
                ShipState sea = fleet.Helm.CreateShip(SeaSprite);
                check(sea.ShipId == "Sea Sprite" && sea.DefinitionId == "Sea Sprite" && sea.IsInitializing && !sea.IsStored,
                    "Enabled persistence gates the canonical ship before any default is applied.");
                SavedShipState? saved = await fleet.Persistence.LoadState(sea.ShipId, sea.ShipName);
                check(saved == null && fleet.Storage.InitializeDefaultStorage(sea, saved, fleet.Southport),
                    "A fresh persistence store initializes Sea Sprite at Southport.");
                fleet.Navigation.CancelNavigation(sea);
                check(sea.IsStored && sea.StoredPortId == "southport" && !sea.Underway && !sea.IsNavigating &&
                    sea.HelmsmanPCKey == null && sea.DestinationAreaResRef == null && fleet.Navigation.GetRoute(sea) == null,
                    "Fresh stored state has no helm, movement or autonomous route.");
                check(sea.AreaResRef == "ocean_01" && sea.X == 90 && sea.Y == 100 && sea.Z == SeaSprite.Z &&
                    sea.Hull == 100 && sea.MaximumHull == 100 && sea.WeaponResRef == "ship_cannon" &&
                    sea.DeckAreaResRef == "sea_sprite_d2" && sea.SpritePrefix == "sloop" && sea.Heading == SeaSprite.Heading,
                    "Fresh storage uses the configured port anchor and preserves ship equipment and deck definition.");
                check(!fleet.Storage.GetStoredShips(fleet.Registry.GetShips(), "southport").Any(),
                    "Retrieval excludes ships whose initial snapshot is not yet committed.");
                check(await fleet.Persistence.TrySaveInitialState(sea) && sea.IsInitializing,
                    "Initial persistence writes the stored snapshot without prematurely releasing the startup gate.");
                sea.IsInitializing = false;
                check(fleet.Storage.GetStoredShips(fleet.Registry.GetShips(), "southport").Single() == sea &&
                    !fleet.Storage.GetStoredShips(fleet.Registry.GetShips(), "driftwood_isle").Any(),
                    "The committed Sea Sprite is discoverable only at its storage port.");
                ShipState nearby = fleet.Helm.CreateShip(Definitions.Single(d => d.ShipName == "Stormrunner"));
                nearby.IsInitializing = false; nearby.X = sea.X; nearby.Y = sea.Y;
                ShipEncounterService encounters = new(); encounters.CheckEncounters(fleet.Registry.GetShips());
                check(encounters.GetActiveEncounters().Count == 0 &&
                    !new ShipVisibilityService().GetVisibleShips(nearby, fleet.Registry.GetShips()).Any(c => c.Ship == sea),
                    "Stored Sea Sprite is excluded from encounters and chart visibility even at zero distance.");
                foreach (ShipDefinition definition in Definitions.Where(d => d != SeaSprite))
                {
                    ShipState other = fleet.Registry.GetShip(definition.ShipName) ?? fleet.Helm.CreateShip(definition);
                    // Restore the test observer's original position before checking the definition.
                    if (other == nearby) { other.X = definition.X; other.Y = definition.Y; }
                    check(!fleet.Storage.InitializeDefaultStorage(other, null, null) && !other.IsStored &&
                        other.StoredPortId == null && other.X == definition.X && other.Y == definition.Y &&
                        other.Hull == definition.Hull && other.WeaponResRef == definition.WeaponResRef,
                        $"{definition.ShipName} retains its existing initial state.");
                }
            }

            using (Fleet restart = new(options, filesFactory))
            {
                ShipState sea = restart.Helm.CreateShip(SeaSprite);
                SavedShipState saved = (await restart.Persistence.LoadState(sea.ShipId))!;
                check(ShipSnapshotCodec.TryRestore(sea, saved, out _) && sea.IsStored && sea.StoredPortId == "southport",
                    "A new service graph restores Sea Sprite's stored snapshot after restart.");
                check(!restart.Storage.InitializeDefaultStorage(sea, saved, restart.Southport),
                    "Restart never reapplies the bootstrap default over a stored record.");
                sea.IsInitializing = false;
                check(!restart.Storage.RetrieveShip(sea, "driftwood_isle") && sea.IsStored,
                    "Retrieval cannot activate Sea Sprite at a different shipyard.");
                check(restart.Storage.RetrieveShip(sea, "southport") && sea.IsActiveWorld && sea.StoredPortId == null,
                    "The existing storage transition activates Sea Sprite on retrieval.");
                // ShipPortService invokes this same persistence path after native
                // transfer/aboard tracking succeeds. No teleport is simulated here.
                check(await restart.Persistence.TrySaveState(sea) && await restart.Persistence.FlushAsync(),
                    "The retrieved active state is committed by the existing save queue.");
            }
            using (Fleet restart = new(options, filesFactory))
            {
                ShipState sea = restart.Helm.CreateShip(SeaSprite);
                SavedShipState saved = (await restart.Persistence.LoadState(sea.ShipId))!;
                check(ShipSnapshotCodec.TryRestore(sea, saved, out _) && !sea.IsStored && sea.StoredPortId == null &&
                    !restart.Storage.InitializeDefaultStorage(sea, saved, restart.Southport),
                    "A subsequent restart keeps the retrieved ship active instead of restoring the docked default.");
                sea.IsInitializing = false;
                check(sea.IsActiveWorld && !restart.Storage.GetStoredShips(restart.Registry.GetShips(), "southport").Any(),
                    "The retrieved ship is no longer offered by the shipyard after restart.");
            }

            foreach (bool stored in new[] { false, true })
            {
                options = Options(stored ? "existing-stored" : "existing-active");
                using (Fleet original = new(options, filesFactory))
                {
                    ShipState sea = original.Helm.CreateShip(SeaSprite);
                    sea.IsInitializing = false; sea.X = 211; sea.Y = 177; sea.Hull = 71;
                    sea.Heading = Heading.SouthWest; sea.Cargo.Add(new() { ItemId = "timber", Quantity = 5 });
                    sea.WeaponResRef = "ship_ballista";
                    if (stored) original.Storage.StoreShip(sea, "driftwood_isle");
                    else sea.Underway = sea.IsNavigating = true;
                    check(await original.Persistence.TrySaveState(sea), "Pre-existing Sea Sprite fixture is durably saved.");
                }
                byte[] before = File.ReadAllBytes(ShipFile(options.DataDirectory!));
                using Fleet existing = new(options, filesFactory);
                ShipState restored = existing.Helm.CreateShip(SeaSprite);
                SavedShipState saved = (await existing.Persistence.LoadState(restored.ShipId))!;
                check(ShipSnapshotCodec.TryRestore(restored, saved, out _) &&
                    !existing.Storage.InitializeDefaultStorage(restored, saved, existing.Southport),
                    "A valid existing record takes precedence over the configured default.");
                check(restored.IsStored == stored && restored.StoredPortId == (stored ? "driftwood_isle" : null) &&
                    restored.X == 211 && restored.Y == 177 && restored.Hull == 71 && restored.Heading == Heading.SouthWest &&
                    restored.WeaponResRef == "ship_ballista" && restored.Cargo.Single().Quantity == 5,
                    "Existing active/stored position, port, hull, equipment and cargo remain unchanged.");
                check(!await existing.Persistence.TrySaveInitialState(restored) &&
                    File.ReadAllBytes(ShipFile(options.DataDirectory!)).SequenceEqual(before),
                    "One-time seeding refuses an existing record without altering its persisted bytes.");
            }

            options = Options("invalid");
            using (Fleet invalid = new(options, filesFactory))
            {
                ShipState sea = invalid.Helm.CreateShip(SeaSprite);
                Directory.CreateDirectory(Path.GetDirectoryName(ShipFile(options.DataDirectory!))!);
                File.WriteAllText(ShipFile(options.DataDirectory!), "{invalid");
                bool rejected = false;
                try { await invalid.Persistence.LoadState(sea.ShipId); } catch (JsonException) { rejected = true; }
                check(rejected && !await invalid.Persistence.TrySaveInitialState(sea) && sea.IsInitializing &&
                    File.ReadAllText(ShipFile(options.DataDirectory!)) == "{invalid",
                    "Malformed persistence is blocked and never overwritten by the new default.");
                foreach (string payload in new[] { "{}", "null", "{incomplete" })
                {
                    SavedShipState incomplete = ShipSnapshotCodec.Capture(sea); incomplete.SnapshotJson = payload;
                    string bytes = JsonSerializer.Serialize(incomplete);
                    File.WriteAllText(ShipFile(options.DataDirectory!), bytes);
                    SavedShipState saved = (await invalid.Persistence.LoadState(sea.ShipId))!;
                    check(!ShipSnapshotCodec.TryRestore(sea, saved, out _) &&
                        !invalid.Storage.InitializeDefaultStorage(sea, saved, invalid.Southport) &&
                        !await invalid.Persistence.TrySaveInitialState(sea) &&
                        File.ReadAllText(ShipFile(options.DataDirectory!)) == bytes,
                        "Incomplete or invalid snapshot payload is preserved for administrator repair.");
                }
                check(!invalid.Storage.GetStoredShips(invalid.Registry.GetShips(), "southport").Any() &&
                    !await invalid.Persistence.TrySaveState(sea),
                    "Invalid initialization cannot be retrieved or overwritten by ordinary saves.");
            }

            options = Options("write-failure");
            Directory.CreateDirectory(options.DataDirectory!);
            File.WriteAllText(Path.Combine(options.DataDirectory!, "ships"), "preserve this blocking file");
            using (Fleet failed = new(options, filesFactory))
            {
                ShipState sea = failed.Helm.CreateShip(SeaSprite);
                failed.Storage.InitializeDefaultStorage(sea, null, failed.Southport);
                check(!await failed.Persistence.TrySaveInitialState(sea) && sea.IsInitializing &&
                    !failed.Storage.GetStoredShips(failed.Registry.GetShips(), "southport").Any() &&
                    File.ReadAllText(Path.Combine(options.DataDirectory!, "ships")) == "preserve this blocking file",
                    "Failed initial persistence leaves retrieval gated and existing filesystem data intact.");
            }

            SailingPersistenceContextFactory database = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseInMemoryDatabase("sea-sprite-" + Guid.NewGuid()).Options);
            ShipPersistenceOptions dbOptions = new() { Backend = SailingPersistenceBackend.PostgreSql, DataDirectory = directory };
            using (Fleet freshDatabase = new(dbOptions, database))
            {
                ShipState sea = freshDatabase.Helm.CreateShip(SeaSprite);
                SavedShipState? saved = await freshDatabase.Persistence.LoadState(sea.ShipId, sea.ShipName);
                check(saved == null && freshDatabase.Storage.InitializeDefaultStorage(sea, saved, freshDatabase.Southport) &&
                    await freshDatabase.Persistence.TrySaveInitialState(sea),
                    "The same default seeding path works with the existing database integration.");
                sea.IsInitializing = false;
                freshDatabase.Storage.RetrieveShip(sea, "southport");
                check(await freshDatabase.Persistence.TrySaveState(sea), "Database retrieval persists the active snapshot.");
            }
            using (Fleet restartedDatabase = new(dbOptions, database))
            {
                ShipState sea = restartedDatabase.Helm.CreateShip(SeaSprite);
                SavedShipState saved = (await restartedDatabase.Persistence.LoadState(sea.ShipId))!;
                check(ShipSnapshotCodec.TryRestore(sea, saved, out _) && !sea.IsStored &&
                    !restartedDatabase.Storage.InitializeDefaultStorage(sea, saved, restartedDatabase.Southport),
                    "A retrieved database ship also survives restart without being redocked.");
            }
            database = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseInMemoryDatabase("sea-sprite-legacy-" + Guid.NewGuid()).Options);
            using (SailingPersistenceDbContext context = database.CreateDbContext())
            {
                context.SavedShipStates.Add(new()
                { ShipName = "Sea Sprite", AreaResRef = "ocean_01", X = 22, Y = 24, Heading = Heading.North, Hull = 64 });
                await context.SaveChangesAsync();
            }
            using (Fleet legacy = new(dbOptions, database))
            {
                ShipState sea = legacy.Helm.CreateShip(SeaSprite);
                SavedShipState saved = (await legacy.Persistence.LoadState(sea.ShipId, sea.ShipName))!;
                check(saved.ShipId == null && saved.SnapshotJson == null && ShipSnapshotCodec.TryRestore(sea, saved, out _) &&
                    !sea.IsStored && sea.X == 22 && sea.Y == 24 && sea.Hull == 64 &&
                    !legacy.Storage.InitializeDefaultStorage(sea, saved, legacy.Southport) &&
                    !await legacy.Persistence.TrySaveInitialState(sea),
                    "A valid legacy active row is preserved rather than mistaken for a fresh Sea Sprite.");
            }

            using (Fleet disabled = new(new() { Backend = SailingPersistenceBackend.Disabled, DataDirectory = directory }, filesFactory))
            {
                ShipState sea = disabled.Helm.CreateShip(SeaSprite);
                check(sea.IsStored && sea.StoredPortId == "southport" && !sea.IsInitializing && !sea.IsActiveWorld,
                    "Explicitly disabled persistence still initializes the configured stored state without I/O.");
                bool rejected = false;
                try { disabled.Helm.CreateShip(SeaSprite with { ShipName = "bad-port", HelmTag = "bad-port-helm", InitialStoredPortId = "missing-port" }); }
                catch (ArgumentException) { rejected = true; }
                check(rejected && disabled.Registry.GetShips().Count == 1,
                    "Unknown default storage ports fail before registering a partially initialized ship.");
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
#endif
