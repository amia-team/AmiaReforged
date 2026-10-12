#if SAILING_REGRESSION
using System.Reflection;
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services.Sailing;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

static class DockyardNuiChecks
{
    private static ShipState Ship(string id, string name) => new()
    { ShipId = id, ShipName = name, DeckAreaResRef = "sea_sprite_d2", AreaResRef = "ocean_01", SpritePrefix = "sloop" };

    public static void Run(Action<bool, string> check)
    {
        ShipRegistryService registry = new(); ShipStorageService storage = new();
        ShipState sea = Ship("Sea Sprite", "Sea Sprite"), pearl = Ship("Black Pearl", "Black Pearl"), foreign = Ship("foreign", "Foreign port");
        registry.Register(sea); registry.Register(pearl); registry.Register(foreign);
        storage.StoreShip(sea, "southport"); storage.StoreShip(foreign, "driftwood_isle");
        DockyardShipList View() => DockyardShipList.Create(registry, storage, "southport", _ => true);
        var first = View();
        check(first.Rows.Count == 1 && first.Rows[0].Name == "Sea Sprite" && first.AvailableCount == 1,
            "Southport stored query populates a Sea Sprite display row on initial open.");
        check(first.Rows[0].ShipId == "Sea Sprite" && first.Rows[0].Vessel == "Sloop / Communal" &&
            first.Rows[0].Status == "STORED / AVAILABLE FOR RETRIEVAL" && first.Rows[0].CanRetrieve,
            "Ship name, class, communal category and explicit stored/available status share the same rendered row.");
        check(first.TryResolve(0, registry, storage, "southport", _ => true, out var selected) && ReferenceEquals(selected, sea),
            "A rendered Sea Sprite retrieval button resolves that exact canonical ship.");
        check(!first.TryResolve(-1, registry, storage, "southport", _ => true, out _) &&
            !first.TryResolve(1, registry, storage, "southport", _ => true, out _), "Invalid/absent NUI array indices resolve no ship.");
        check(!first.TryResolve(0, registry, storage, "driftwood_isle", _ => true, out _) &&
            !first.TryResolve(0, registry, storage, "southport", _ => false, out _), "Click resolution rechecks current port and actor access.");

        storage.StoreShip(pearl, "southport");
        var multiple = View();
        check(multiple.Rows.Count == 2 && multiple.Rows.Select(r => r.Name).SequenceEqual(["Black Pearl", "Sea Sprite"]),
            "Refresh displays multiple stored ships in distinct, deterministic rows.");
        check(first.TryResolve(0, registry, storage, "southport", _ => true, out selected) && ReferenceEquals(selected, sea) &&
            multiple.TryResolve(0, registry, storage, "southport", _ => true, out selected) && ReferenceEquals(selected, pearl),
            "A newly docked ship never changes the identity addressed by an already rendered row.");
        sea.ShipName = "Sea Sprite renamed";
        check(first.Rows[0].Name == "Sea Sprite" && first.TryResolve(0, registry, storage, "southport", _ => true, out selected) && selected == sea,
            "Display-name changes do not replace a row's stable ship identity or canonical reference.");
        check(View().Rows.Single(r => r.ShipId == sea.ShipId).Name == "Sea Sprite renamed", "Refresh picks up the current display name.");
        check(storage.RetrieveShip(sea, "southport") && !first.TryResolve(0, registry, storage, "southport", _ => true, out _) &&
            View().Rows.All(r => r.ShipId != sea.ShipId), "Successful storage retrieval removes Sea Sprite from new views and invalidates its old row.");
        check(multiple.TryResolve(0, registry, storage, "southport", _ => true, out selected) && selected == pearl,
            "Removing one stored ship does not alter another rendered row's retrieval identity.");

        sea.ShipName = "Sea Sprite"; storage.StoreShip(sea, "southport");
        var stale = View(); int seaIndex = stale.Rows.ToList().FindIndex(r => r.ShipId == sea.ShipId);
        registry.Remove(sea);
        var replacement = Ship("Sea Sprite", "Sea Sprite"); storage.StoreShip(replacement, "southport"); registry.Register(replacement);
        check(!stale.TryResolve(seaIndex, registry, storage, "southport", _ => true, out _),
            "A registry replacement with the same stable ID cannot be retrieved using a stale instance row.");
        check(View().Rows.Single(r => r.ShipId == replacement.ShipId).Ship == replacement,
            "A refreshed window obtains the canonical replacement explicitly.");
        replacement.IsInitializing = true;
        check(View().Rows.All(r => r.ShipId != replacement.ShipId), "Incomplete startup restoration is excluded from the dockyard query.");
        replacement.IsInitializing = false; replacement.IsDocking = true;
        check(View().Rows.All(r => r.ShipId != replacement.ShipId), "Ships in a port transfer do not appear as available stored rows.");
        replacement.IsDocking = false; replacement.IsEconomyBusy = true;
        var busy = View().Rows.Single(r => r.ShipId == replacement.ShipId);
        check(!busy.CanRetrieve && !busy.CanTrade && busy.Status == "STORED / BUSY", "Economic reservations render a disabled, clearly busy stored row.");
        replacement.IsEconomyBusy = false;

        registry.Remove(replacement);
        replacement = new() { ShipId = "test-dock-cutter", ShipName = "Test Dock Cutter", DefinitionId = CutterRentalFleet.DefinitionId,
            AllowsWeapons = false, DeckAreaResRef = "test_cut_deck", AreaResRef = "ocean_01", RentalPoolId = "southport" };
        storage.StoreShip(replacement, "southport"); registry.Register(replacement);
        check(View().Rows.All(r => r.ShipId != replacement.ShipId) && ShipRentalService.Available(replacement, "southport"),
            "A free rental pool vessel is a rental offer, not a communal retrieval row.");
        replacement.Rental = new() { ShipId = replacement.ShipId, RenterAccount = "owner", Status = ShipRentalStatus.Active };
        var ownerView = DockyardShipList.Create(registry, storage, "southport", s => ShipAccessService.MayCommand(s, "owner"));
        var owned = ownerView.Rows.Single(r => r.ShipId == replacement.ShipId);
        check(owned.CanRetrieve && owned.Vessel == "Cutter / Your rental" && owned.Status == DockyardShipList.AvailableStatus &&
            !ShipRentalService.Available(replacement, "southport"), "An active stored rental appears for owner retrieval, separate from new rental availability.");
        var intruderView = DockyardShipList.Create(registry, storage, "southport", s => ShipAccessService.MayCommand(s, "intruder"));
        var blocked = intruderView.Rows.Single(r => r.ShipId == replacement.ShipId);
        check(!blocked.CanRetrieve && blocked.Vessel == "Cutter / Other rental" && blocked.Status == "STORED / UNAVAILABLE TO YOU" &&
            !intruderView.TryResolve(intruderView.Rows.ToList().IndexOf(blocked), registry, storage, "southport", _ => true, out _),
            "Unavailable stored rentals are named explicitly, with disabled actions that cannot be forged through a stale row.");
        replacement.Rental = null;
        check(View().Rows.All(r => r.ShipId != replacement.ShipId), "Return settlement moves the vessel out of retrieval rows and back to its rental offer.");

        ShipRegistryService restarted = new();
        var restored = Ship(pearl.ShipId, pearl.ShipName);
        check(ShipSnapshotCodec.TryRestore(restored, ShipSnapshotCodec.Capture(pearl), out _), "Stored ship snapshot restores successfully for a dockyard restart.");
        restarted.Register(restored);
        var restoredView = DockyardShipList.Create(restarted, storage, "southport", _ => true);
        check(restoredView.Rows.Single().Name == "Black Pearl" && restoredView.Rows.Single().Status == DockyardShipList.AvailableStatus &&
            restoredView.TryResolve(0, restarted, storage, "southport", _ => true, out selected) && selected == restored,
            "Persisted ships populate the same named, stored, identity-safe rows after restart.");
        storage.RetrieveShip(pearl, "southport");
        var empty = View();
        check(empty.Rows.Count == 0 && empty.AvailableCount == 0 && empty.Summary.Contains("No stored ships"),
            "An empty retrieval section has zero rows and an explicit empty-state message, even when rental offers exist.");
        check(!View().Rows.Any(r => r.ShipId == foreign.ShipId), "Other-port ships never appear in the Southport retrieval list.");

        VerifyLayout(check);
    }

    private static void VerifyLayout(Action<bool, string> check)
    {
        // Exercise the actual layout factory through the native-free constructor graph.
        // This verifies managed cell geometry and bindings, not engine rendering or SetBindValues calls.
        Dictionary<Type, object> services = new()
        {
            [typeof(EventService)] = new EventService([]),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
            [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options)
        };
        try
        {
            var service = (ShipRetrievalNuiService)StartupChecks.CreateResolver(services)(typeof(ShipRetrievalNuiService));
            var layout = (NuiColumn)typeof(ShipRetrievalNuiService).GetMethod("CreateLayout", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(service, ["southport"])!;
            NuiList[] lists = layout.Children.OfType<NuiList>().ToArray();
            check(lists.Length == 3 && lists.Select(l => ((NuiBind<int>)l.RowCount).Key).SequenceEqual(
                ["dockyard_stored_count", "dockyard_rental_count", "dockyard_lease_count"]),
                "Stored retrieval, new rentals and active agreements have independent bound row counts.");
            var stored = lists[0];
            check(stored.Width == 920 && stored.Height == 130 && stored.RowHeight == 34 &&
                stored.RowTemplate.All(c => c.Width > 0 && !c.VariableSize), "Actual retrieval layout fixes list, row and cell geometry.");
            check(stored.RowTemplate.Take(3).Select(c => ((NuiBind<string>)((NuiLabel)c.Element).Label).Key).SequenceEqual(
                ["dockyard_stored_names", "dockyard_stored_vessels", "dockyard_stored_statuses"]) &&
                stored.RowTemplate.Take(3).All(c => c.Element.Height == 22),
                "Name, vessel/category and stored status use per-row string binds with visible label heights.");
            var retrieve = (NuiButton)stored.RowTemplate[3].Element;
            check(retrieve.Id == "dockyard_retrieve" && ((NuiBind<bool>)retrieve.Enabled!).Key == "dockyard_retrieve_enabled" &&
                stored.RowTemplate[4].Element.Id == "dockyard_market", "List action templates use stable IDs and per-row authorization binds.");
            check(layout.Children.OfType<NuiLabel>().All(l => l.Height == 22), "Dockyard headings, summaries and terms no longer depend on unspecified label heights.");
        }
        finally { foreach (var service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
    }
}
#endif
