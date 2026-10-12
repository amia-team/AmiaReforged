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
            first.Rows[0].Status == "STORED / AVAILABLE FOR RETRIEVAL" && first.Rows[0].CanRetrieve && first.Rows[0].Hull.Contains("Hull"),
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
        check(ownerView.Rows.All(r => r.ShipId != replacement.ShipId) && !ShipRentalService.Available(replacement, "southport"),
            "An active stored rental stays in the agreement section, separate from communal retrieval and new offers.");
        var intruderView = DockyardShipList.Create(registry, storage, "southport", s => ShipAccessService.MayCommand(s, "intruder"));
        check(intruderView.Rows.All(r => r.ShipId != replacement.ShipId), "Another account's rentals never appear in communal rows.");
        var agreement = DockyardRentalAgreement.Create(replacement.Rental, replacement, true, true);
        check(agreement.Name.Contains("Cutter") && agreement.Name.Contains("Hull") && agreement.Name.Contains("STORED") &&
            agreement.Terms.Contains("Fee:") && agreement.Terms.Contains("Deposit:") && agreement.Eligibility.Contains("RETURN ELIGIBLE") &&
            agreement.CanRetrieve && agreement.CanReturn, "Agreement rows expose rental status, class, hull, fee, deposit and independent retrieval/return eligibility.");
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

    private static void VerifyLayout(Action<bool, string> check) => DockyardDropdownChecks.BasicLayout(check);
}

// Tests actual native-layout definitions and row projections; no live client rendering is inferred.
static class DockyardLayoutRecoveryChecks
{
    private static IEnumerable<NuiElement> Elements(NuiElement element) => NauticalChartRecoveryChecks.Elements(element);
    private static string[] Keys(NuiElement element) => Elements(element).OfType<NuiLabel>()
        .Select(l => l.Label).OfType<NuiBind<string>>().Select(b => b.Key).ToArray();
    private static ShipState Stored(string id, string name, ShipRegistryService registry, ShipStorageService storage)
    {
        var ship = new ShipState { ShipId = id, ShipName = name, SpritePrefix = "sloop", DeckAreaResRef = "deck", AreaResRef = "ocean_01" };
        registry.Register(ship); storage.StoreShip(ship, "southport"); return ship;
    }
    private static ShipState Cutter(RentalVesselOption slot) => new()
    {
        ShipId = slot.ShipId, ShipName = slot.ShipName, DefinitionId = CutterRentalFleet.DefinitionId,
        RentalPoolId = slot.PortId, IsStored = true, StoredPortId = slot.PortId, AllowsWeapons = false,
        MaximumHull = slot.MaximumHull, Hull = slot.MaximumHull, CargoCapacity = slot.CargoCapacity,
        DeckAreaResRef = slot.DeckAreaResRef, AreaResRef = "ocean_01", SpritePrefix = slot.SpritePrefix
    };

    public static void Run(Action<bool, string> check)
    {
        Rows(check);
        DockyardDropdownChecks.Run(check);
    }

    private static void Rows(Action<bool, string> check)
    {
        var registry = new ShipRegistryService(); var storage = new ShipStorageService();
        var sea = Stored("Sea Sprite", "Sea Sprite", registry, storage);
        var initial = DockyardShipList.Create(registry, storage, "southport", _ => true);
        var row = initial.Rows.Single();
        check(row.Name == "Sea Sprite" && row.Vessel == "Sloop / Communal" && row.Hull == "Hull 100/100" &&
            row.Status == DockyardShipList.AvailableStatus && row.StoredPort == "Stored port: southport" &&
            row.MarketStatus == "Cargo / market available" && row.CanRetrieve && row.CanTrade,
            "The first stored row displays name, class, hull, communal status, stored port, availability and both eligible actions.");
        var pearl = Stored("Black Pearl", "Black Pearl", registry, storage);
        var refreshed = DockyardShipList.Create(registry, storage, "southport", _ => true);
        check(initial.TryResolve(0, registry, storage, "southport", _ => true, out var original) && original == sea &&
            refreshed.TryResolve(0, registry, storage, "southport", _ => true, out var next) && next == pearl &&
            refreshed.Rows[1].ShipId == sea.ShipId, "Multiple rows and refresh preserve the exact canonical ship behind each rendered action.");
        sea.ShipName = "Sea Sprite renamed"; sea.Hull = 65;
        check(row.Name == "Sea Sprite" && row.Hull == "Hull 100/100" &&
            DockyardShipList.Create(registry, storage, "southport", _ => true).Rows[1].Hull == "Hull 65/100",
            "Row text is an immutable display snapshot; refresh updates hull and name without replacing identity.");
        storage.RetrieveShip(sea, "southport");
        check(!initial.TryResolve(0, registry, storage, "southport", _ => true, out _) &&
            DockyardShipList.Create(registry, storage, "southport", _ => true).Rows.Single().ShipId == pearl.ShipId,
            "An old retrieval click is rejected after the ship leaves storage; unrelated rows stay available.");

        var slots = CutterRentalFleet.DefaultSlots(); var cutter = Cutter(slots[0]);
        string snapshot = System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(cutter));
        var offer = DockyardRentalOffer.Create(slots[0], cutter, true, true, false);
        check(offer.ShipId == slots[0].ShipId && offer.DisplayName == "Southport Cutter 01" && offer.Vessel == "Cutter / UNARMED" &&
            offer.HullCargo == "Hull 75/75; cargo 60" && offer.Fee == "Fee: 100 gp" && offer.Deposit == "Deposit: 200 gp" &&
            offer.Damage == "Damage: 2 gp/hull" && offer.Availability == "AVAILABLE FOR RENT" && offer.CanRent,
            "Cutter 01 shows its name, hull, cargo capacity, unarmed class, fee, deposit and rent availability in short label lines.");
        var disabled = DockyardRentalOffer.Create(slots[0], cutter, false, true, false);
        check(!disabled.CanRent && disabled.Availability == "CHECKOUT DISABLED" && disabled.Reason == "Rentals disabled or pending" &&
            disabled.Detail == "server/resource validation." && disabled.Fee == offer.Fee && disabled.Deposit == offer.Deposit,
            "Disabled rentals retain visible terms and an explicit reason, rather than hiding the row or relying on a tooltip.");
        var missing = DockyardRentalOffer.Create(slots[0], null, true, false, false);
        check(!missing.CanRent && missing.DisplayName == slots[0].ShipName && missing.Reason == "Vessel not registered" &&
            missing.HullCargo.Contains("75") && missing.HullCargo.Contains("60"), "Missing vessels retain configured names, specifications and a visible unavailability reason.");
        check(DockyardRentalOffer.Create(slots[0], cutter, true, false, false) is { CanRent: false, Reason: "Module resources missing" } &&
            DockyardRentalOffer.Create(slots[0], cutter, true, true, true) is { CanRent: false, Reason: "Vessel occupied" },
            "Resource and occupant restrictions remain enforced and are readable in the offer row.");
        cutter.IsInitializing = true;
        check(DockyardRentalOffer.Create(slots[0], cutter, true, true, false) is { CanRent: false, Reason: "Restoration pending" },
            "Restoring cutter offers do not become enabled through the presentation changes.");
        cutter.IsInitializing = false;
        cutter.IsEconomyBusy = true;
        check(DockyardRentalOffer.Create(slots[0], cutter, true, true, true) is { CanRent: false, Reason: "Vessel busy" },
            "An ineligible offer's placeholder occupant flag cannot hide its actual economic reservation reason.");
        cutter.IsEconomyBusy = false; cutter.Hull = 0;
        check(DockyardRentalOffer.Create(slots[0], cutter, true, true, true) is { CanRent: false, Reason: "Hull disabled" },
            "A disabled hull displays its actual reason even when occupancy was not queried for an already-ineligible offer.");
        cutter.Hull = cutter.MaximumHull; cutter.Cargo.Add(new MerchantCargo { ItemId = "grain", Quantity = 1 });
        check(DockyardRentalOffer.Create(slots[0], cutter, true, true, true) is { CanRent: false, Reason: "Cargo must be unloaded" },
            "A loaded rental cannot be checked out and its visible cargo reason does not falsely claim that passengers were found.");
        cutter.Cargo.Clear(); cutter.IsDocking = true;
        check(DockyardRentalOffer.Create(slots[0], cutter, true, true, true) is { CanRent: false, Reason: "Dock transfer pending" },
            "Docking offers retain the transfer gate and show docking rather than a placeholder occupied reason.");
        cutter.IsDocking = false;
        var lease = new ShipRentalRecord { RentalId = "layout-lease", ShipId = cutter.ShipId, RenterAccount = "renter",
            PortId = "southport", Status = ShipRentalStatus.Active, Fee = 100, Deposit = 200, DamageRate = 2 };
        cutter.Rental = lease;
        var agreement = DockyardRentalAgreement.Create(lease, cutter, true, true);
        check(agreement.DisplayName == slots[0].ShipName && agreement.State == "Active / STORED" && agreement.Hull == "Cutter / Hull 75/75" &&
            agreement.Fee == "Fee: 100 gp" && agreement.Deposit == "Deposit: 200 gp" && agreement.ReturnStatus == "RETURN ELIGIBLE" &&
            agreement.ReturnReason == "Docked, empty, unoccupied." && agreement.CanRetrieve && agreement.CanReturn,
            "An active agreement exposes name, active/stored state, hull, paid terms and return eligibility separately from its actions.");
        check(!DockyardRentalOffer.Create(slots[0], cutter, true, true, false).CanRent &&
            DockyardRentalOffer.Create(slots[0], cutter, true, true, false).Reason == "Reserved / active agreement",
            "An existing rental never becomes a new checkout offer due to the row redesign.");
        cutter.IsStored = false;
        var atSea = DockyardRentalAgreement.Create(lease, cutter, false, false);
        check(atSea.State == "Active / AT SEA" && !atSea.CanRetrieve && !atSea.CanReturn &&
            atSea.ReturnReason == "Dock at Southport" && atSea.ReturnDetail == "before returning the ship.",
            "An agreement at sea displays the docking requirement without enabling retrieval or return.");
        cutter.IsStored = true; cutter.Cargo.Add(new MerchantCargo { ItemId = "grain", Quantity = 1 });
        check(DockyardRentalAgreement.Create(lease, cutter, true, false) is { ReturnReason: "Unload cargo before return.", ReturnDetail: "Use CARGO / MARKET.", CanReturn: false },
            "Cargo prevents return while keeping the existing market action available through retrieval eligibility.");
        cutter.Cargo.Clear(); cutter.Rental = null;
        check(snapshot == System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(cutter)),
            "Repeated offer/agreement projections never mutate ship, cargo, rental or persisted snapshot state.");
        registry.Register(cutter);
        check(DockyardShipList.Create(registry, storage, "southport", _ => true).Rows.All(r => r.ShipId != cutter.ShipId),
            "Communal rows remain independent of both rental offer identities and agreement identities.");
        var other = Cutter(slots[1]);
        var offers = new[] { DockyardRentalOffer.Create(slots[0], cutter, true, true, false), DockyardRentalOffer.Create(slots[1], other, true, true, false) };
        check(offers.Select(o => o.ShipId).SequenceEqual(slots.Select(s => s.ShipId)) && offers[0].DisplayName != offers[1].DisplayName,
            "Multiple checkout rows retain the copied definition order and distinct captured vessel IDs.");
    }

}

// Actual dropdown state/projections and managed Anvil layouts. Native event delivery requires NWN.
static class DockyardDropdownChecks
{
    private sealed class Fleet
    {
        public readonly ShipRegistryService Registry = new();
        public readonly ShipStorageService Storage = new();
        public readonly List<RentalVesselOption> Offers = CutterRentalFleet.DefaultSlots();
        public readonly List<ShipRentalRecord> Agreements = [];
        public readonly DockyardSelectionState State = new("southport", "owner");
        public ShipState Sea;
        public Fleet()
        {
            Sea = Add("Sea Sprite", "Sea Sprite");
            foreach (var offer in Offers) Add(offer.ShipId, offer.ShipName, offer);
            Refresh();
        }
        public ShipState Add(string id, string name, RentalVesselOption? slot = null)
        {
            var ship = new ShipState { ShipId = id, ShipName = name, SpritePrefix = "sloop", DeckAreaResRef = "fixture_deck_" + Registry.GetShips().Count, AreaResRef = "ocean_01",
                DefinitionId = slot == null ? null : CutterRentalFleet.DefinitionId, RentalPoolId = slot?.PortId,
                AllowsWeapons = slot == null, Hull = slot?.MaximumHull ?? 100, MaximumHull = slot?.MaximumHull ?? 100,
                CargoCapacity = slot?.CargoCapacity ?? 80 };
            Registry.Register(ship); Storage.StoreShip(ship, "southport"); return ship;
        }
        public void Refresh(DockyardSelectionState? state = null) => (state ?? State).Refresh(Registry,
            DockyardShipList.Create(Registry, Storage, "southport", _ => true), Offers, Agreements);
        public ShipRentalRecord Rent(int index)
        {
            var ship = Registry.GetShip(Offers[index].ShipId)!;
            var rental = new ShipRentalRecord { RentalId = "agreement-" + index, ShipId = ship.ShipId, PortId = "southport", RenterAccount = "owner",
                Status = ShipRentalStatus.Active, Fee = 100, Deposit = 200, CheckoutHull = ship.Hull };
            ship.Rental = rental; Agreements.Add(rental); return rental;
        }
        public DockyardSelectedVessel Details(bool enabled = true, bool resources = true, bool occupants = false, bool retrieve = true,
            bool trade = true, bool agreementCurrent = true, bool offerCurrent = true, ShipRentalRecord? current = null) =>
            DockyardSelectedVessel.Capture(State, Registry, Storage, _ => retrieve, enabled, _ => resources, _ => occupants, _ => trade,
                agreementCurrent, offerCurrent, current);
    }
    private static IEnumerable<NuiElement> Elements(NuiElement element) => NauticalChartRecoveryChecks.Elements(element);
    private static Dictionary<Type, object> Services() => new()
    {
        [typeof(EventService)] = new EventService([]),
        [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
        [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(new DbContextOptionsBuilder<SailingPersistenceDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options)
    };
    private static NuiColumn Layout(ShipRetrievalNuiService service, DockyardSelectionState state) =>
        (NuiColumn)typeof(ShipRetrievalNuiService).GetMethod("CreateLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, ["southport", state])!;
    public static void BasicLayout(Action<bool, string> check)
    {
        var services = Services();
        try
        {
            var service = (ShipRetrievalNuiService)StartupChecks.CreateResolver(services)(typeof(ShipRetrievalNuiService));
            var root = Layout(service, new Fleet().State); var elements = Elements(root).ToArray();
            var combos = elements.OfType<NuiCombo>().ToArray();
            check(combos.Length == 2 && combos.Select(c => ((NuiBind<List<NuiComboEntry>>)c.Entries).Key).SequenceEqual(["dockyard_modes", "dockyard_vessels"]),
                "The actual dockyard layout uses two supported Anvil combos with independent dynamic option binds.");
            check(combos.Select(c => ((NuiBind<int>)c.Selected).Key).SequenceEqual(["dockyard_mode", "dockyard_vessel"]),
                "Mode and vessel selection have distinct watched scalar integer binds.");
            check(!elements.Any(e => e is NuiList or NuiText or NuiTextEdit), "There are no ship lists, templates or scrollable short text fields in the dockyard.");
            check(elements.OfType<NuiGroup>().All(g => g.Scrollbars == NuiScrollbars.None), "All actual dockyard groups explicitly disable scrolling.");
            check(elements.OfType<NuiButton>().Any(b => ((NuiValue<string>)b.Label).Value == "RETRIEVE SHIP" && b.Width == 680 && b.Height == 40),
                "Sea Sprite has a full-width RETRIEVE SHIP button outside the dropdown.");
            check(elements.OfType<NuiLabel>().Count(l => l.Label is NuiBind<string> bind && bind.Key.StartsWith("dockyard_detail_")) == 10,
                "Ten normal fixed-height labels expose all selected ship details.");
            check(elements.OfType<NuiButton>().Select(b => b.Id).Distinct().Count() == elements.OfType<NuiButton>().Count(),
                "The rendered dockyard action and footer IDs are unique.");
        }
        finally { foreach (var service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
    }
    public static void Run(Action<bool, string> check)
    { Selection(check); Eligibility(check); Geometry(check); EventContract(check); }
    private static void Selection(Action<bool, string> check)
    {
        var f = new Fleet(); var s = f.State;
        check(s.Mode == DockyardMode.Stored && s.Choices.Single().ShipId == f.Sea.ShipId && s.Selected?.Ship == f.Sea,
            "Stored dropdown dynamically contains Sea Sprite and chooses its valid initial default.");
        var v = f.Details();
        check(v.Lines.Length == 10 && v.Lines[0] == "Ship: Sea Sprite" && v.Lines[1] == "Class: Sloop" && v.Lines[2] == "Hull: 100 / 100" &&
            v.Lines[3] == "Cargo: 0 aboard / 80 capacity" && v.Lines[4].Contains("weapons permitted") && v.Lines[5] == "Storage port: southport" &&
            v.Lines[6].Contains("AVAILABLE FOR RETRIEVAL") && v.Lines[8] == "Ownership: COMMUNAL",
            "The selected stored ship panel presents name, class, hull, cargo, weapons, port, availability and communal ownership.");
        var pearl = f.Add("Black Pearl", "Black Pearl"); int oldSeaValue = s.SelectedValue;
        f.Refresh();
        check(s.Choices.Select(c => c.ShipId).SequenceEqual([pearl.ShipId, f.Sea.ShipId]) && s.Selected?.Ship == f.Sea && s.SelectedValue != oldSeaValue,
            "Reordering on refresh preserves the selected canonical Sea Sprite and replaces the dropdown value.");
        check(!s.Select(oldSeaValue) && s.Selected?.Ship == f.Sea, "A stale dropdown watch cannot change selection after refresh.");
        check(!DockyardSelectedVessel.TryAction(DockyardSelectedVessel.ActionId(DockyardAction.Retrieve, oldSeaValue), s, out _),
            "A queued action from the prior published selection is rejected after refresh.");
        check(s.Select(s.Choices.Single(c => c.ShipId == pearl.ShipId).Value) && s.TryResolve(f.Registry, out var selected) && selected == pearl,
            "Selection resolves the chosen stored vessel's canonical ID, never the dropdown ordinal.");
        pearl.Hull = 53; pearl.Cargo.Add(new MerchantCargo { ItemId = "grain", Quantity = 7 });
        check(f.Details().Lines[0] == "Ship: Black Pearl" && f.Details().Lines[2] == "Hull: 53 / 100" && f.Details().Lines[3].Contains("7 aboard"),
            "Selection and live state changes immediately update the information projection without rebuilding a window.");
        check(!s.ChangeMode(99) && !s.Select(999999) && s.Selected?.Ship == pearl, "Invalid modes and dropdown values do not silently pick another ship.");
        check(s.ChangeMode((int)DockyardMode.Rent) && s.Choices.Select(c => c.ShipId).SequenceEqual(f.Offers.Select(o => o.ShipId)) &&
            s.Selected?.ShipId == f.Offers[0].ShipId, "Rent dropdown is populated from configured offers with the first canonical cutter as default.");
        var firstCutter = s.Selected!.Ship!;
        check(s.Select(s.Choices[1].Value) && s.TryResolve(f.Registry, out selected) && selected == f.Registry.GetShip(f.Offers[1].ShipId) && selected != firstCutter,
            "Checkout selection captures Cutter 02 independently of Cutter 01.");
        check(DockyardSelectedVessel.TryAction(DockyardSelectedVessel.ActionId(DockyardAction.Rent, s.SelectedValue), s, out var action) && action == DockyardAction.Rent &&
            f.Details().Lines[0] == "Ship: Southport Cutter 02", "RENT action resolves the selected second cutter with matching information.");
        int secondValue = s.SelectedValue;
        s.Select(s.Choices[0].Value);
        check(!DockyardSelectedVessel.TryAction(DockyardSelectedVessel.ActionId(DockyardAction.Rent, secondValue), s, out _),
            "A queued Cutter 02 checkout cannot act on Cutter 01 after the player changes selection.");
        f.Offers[0].Fee = 123;
        check(s.Selected!.Offer!.Fee == 100, "Offer terms are captured copies, not mutable configuration aliases.");
        f.Refresh(); check(s.Selected!.Offer!.Fee == 123 && s.Selected.Ship == firstCutter, "Refresh updates terms without changing a still-valid canonical cutter.");
        var a = f.Rent(0); var b = f.Rent(1);
        f.Agreements.Add(new() { RentalId = "foreign", ShipId = pearl.ShipId, PortId = "southport", RenterAccount = "other", Status = ShipRentalStatus.Active });
        f.Agreements.Add(new() { RentalId = "remote", ShipId = pearl.ShipId, PortId = "other-port", RenterAccount = "owner", Status = ShipRentalStatus.Active });
        f.Agreements.Add(new() { RentalId = "closed", ShipId = pearl.ShipId, PortId = "southport", RenterAccount = "owner", Status = ShipRentalStatus.Returned });
        f.Refresh(); s.ChangeMode((int)DockyardMode.ActiveRental);
        check(s.Choices.Count == 2 && s.Choices.Select(c => c.RentalId).SequenceEqual([a.RentalId, b.RentalId]) && s.Selected?.RentalId == a.RentalId,
            "Active dropdown contains only this account's open Southport agreements and selects the first valid default.");
        s.Select(s.Choices.Single(c => c.RentalId == b.RentalId).Value);
        check(s.TryResolve(f.Registry, out selected) && selected == f.Registry.GetShip(b.ShipId) && s.Selected!.RentalId == b.RentalId &&
            f.Details()[DockyardAction.RetrieveRental].Enabled && f.Details()[DockyardAction.ReturnRental].Enabled,
            "Rental retrieval and return both address the specifically selected agreement and canonical cutter.");
        check(s.Choices.All(c => c.ShipId != f.Sea.ShipId), "Active agreement options never mix in Sea Sprite or communal ships.");
        b.RentalId = "new-voyage";
        check(s.Selected!.RentalId != b.RentalId && !f.Details()[DockyardAction.ReturnRental].Enabled,
            "Mutating the backing agreement cannot replace the immutable captured rental identity.");
        f.Refresh(); check(s.Selected == null && s.SelectedValue == -1 && s.Choices.Count == 2,
            "Refresh clears a replaced voyage instead of silently selecting another agreement on the same ship.");
        check(!DockyardSelectedVessel.TryAction("dockyard_return:-1", s, out _), "No action can execute with the empty dropdown value.");
        s.ChangeMode((int)DockyardMode.Stored); s.Select(s.Choices.Single(c => c.ShipId == f.Sea.ShipId).Value);
        f.Storage.RetrieveShip(f.Sea, "southport");
        check(!f.Details()[DockyardAction.Retrieve].Enabled, "Current storage is rechecked even before a dropdown refresh.");
        f.Refresh(); check(s.Selected == null && s.Choices.Single().Ship == pearl, "Retrieval clears the departed selection without choosing Black Pearl.");
        s.Select(s.Choices[0].Value); f.Registry.Remove(pearl);
        var replacement = f.Add(pearl.ShipId, "Replacement");
        check(!s.TryResolve(f.Registry, out _) && !f.Details()[DockyardAction.Retrieve].Enabled,
            "A replacement canonical instance with the same ship ID rejects the old captured selection.");
        f.Refresh(); check(s.Selected == null && s.Choices.Single().Ship == replacement, "Refreshing a replaced ship clears selection and requires explicit reselection.");
        s.Select(s.Choices[0].Value); s.Select(-1); f.Refresh();
        check(s.Selected == null && !f.Details()[DockyardAction.Retrieve].Enabled, "A deliberate empty selection stays empty across refresh.");
        var otherView = new DockyardSelectionState("southport", "owner"); f.Refresh(otherView);
        check(otherView.Selected?.Ship == replacement && s.Selected == null, "Independent player windows do not share selection state.");
        s.Close(); f.Refresh();
        check(s.Closed && s.Selected == null && s.Choices.Count == 0 && !s.Select(1) && !s.ChangeMode(1) && !s.TryResolve(f.Registry, out _) &&
            !DockyardSelectedVessel.TryAction("dockyard_retrieve:1", s, out _), "Close retires the captured mapping and rejects stale selection/action messages.");
        var reopened = new DockyardSelectionState("southport", "owner"); f.Refresh(reopened);
        check(!reopened.Closed && reopened.Selected?.Ship == replacement, "Reopen uses a fresh window mapping with a current valid default.");
        var empty = new DockyardSelectionState("southport", "owner");
        var noShips = new ShipRegistryService(); var noStorage = new ShipStorageService();
        empty.Refresh(noShips, DockyardShipList.Create(noShips, noStorage, "southport", _ => true), [], []);
        foreach (var mode in Enum.GetValues<DockyardMode>())
        {
            empty.ChangeMode((int)mode);
            check(empty.Selected == null && empty.Choices.Count == 0 && empty.EmptyMessage.Length > 20,
                "Each empty mode has no default ship and a clear mode-specific explanation.");
            var emptyDetails = DockyardSelectedVessel.Capture(empty, noShips, noStorage, _ => true, true, _ => true, _ => false, _ => true);
            check(emptyDetails.Lines[0] == "No vessel selected" && emptyDetails.Lines.Skip(1).All(string.IsNullOrEmpty),
                "An empty panel never invents ownership, terms, class or status for a nonexistent selection.");
        }
        var custom = new RentalVesselOption { ShipId = "custom-offer", ShipName = "Custom Cutter", PortId = "southport" };
        empty.Refresh(noShips, DockyardShipList.Create(noShips, noStorage, "southport", _ => true), [custom], []);
        empty.ChangeMode((int)DockyardMode.Rent);
        check(empty.Choices.Single().ShipId == custom.ShipId && empty.Selected?.Name == custom.ShipName && !empty.TryResolve(noShips, out _),
            "Configured missing vessels remain named dropdown options without inventing canonical ships or fleet assets.");
        var missing = DockyardSelectedVessel.Capture(empty, noShips, noStorage, _ => true, true, _ => true, _ => false, _ => true);
        check(!missing[DockyardAction.Rent].Enabled && missing[DockyardAction.Rent].Reason.Contains("not registered"), "Missing vessels display a readable disabled reason.");
    }
    private static void Eligibility(Action<bool, string> check)
    {
        var f = new Fleet(); var s = f.State;
        var stored = f.Details();
        check(stored[DockyardAction.Retrieve] is { Visible: true, Enabled: true } && stored[DockyardAction.Market] is { Visible: true, Enabled: true } &&
            !stored[DockyardAction.Rent].Visible && !stored[DockyardAction.ReturnRental].Visible, "Stored mode exposes only retrieval and supported cargo/market actions.");
        check(!f.Details(retrieve: false)[DockyardAction.Retrieve].Enabled && f.Details(retrieve: false)[DockyardAction.Retrieve].Reason.Length > 0,
            "Denied authorization leaves retrieval disabled with a normal-label explanation.");
        f.Sea.IsEconomyBusy = true;
        check(!f.Details()[DockyardAction.Retrieve].Enabled && !f.Details()[DockyardAction.Market].Enabled, "Busy communal ships cannot be retrieved or traded through dropdown actions.");
        f.Sea.IsEconomyBusy = false;
        check(!f.Details(trade: false)[DockyardAction.Market].Enabled && f.Details()[DockyardAction.Retrieve].Enabled,
            "Market eligibility is independent of retrieval permission.");
        f.Sea.WeaponResRef = "";
        check(f.Details().Lines[4] == "Weapons: None equipped / weapons permitted", "Weapon permission does not falsely imply an equipped weapon.");
        f.Sea.WeaponResRef = "ship_cannon";
        s.ChangeMode((int)DockyardMode.Rent); var cutter = s.Selected!.Ship!;
        var offer = f.Details();
        check(offer[DockyardAction.Rent] is { Visible: true, Enabled: true } && !offer[DockyardAction.Retrieve].Visible && !offer[DockyardAction.Market].Visible &&
            offer.Lines[1] == "Class: Cutter" && offer.Lines[2] == "Hull: 75 / 75" && offer.Lines[3].Contains("60 capacity") && offer.Lines[4] == "Weapons: UNARMED" &&
            offer.Lines[7].Contains("100 gp") && offer.Lines[7].Contains("200 gp"), "Rental mode shows the cutter's unarmed specifications, terms and its standalone checkout action.");
        foreach (var blocked in new[] { f.Details(enabled: false), f.Details(resources: false), f.Details(occupants: true), f.Details(offerCurrent: false) })
            check(!blocked[DockyardAction.Rent].Enabled && blocked[DockyardAction.Rent].Reason.Length > 0,
                "Disabled configuration, missing resources, occupants and changed terms all retain visible checkout explanations.");
        cutter.IsInitializing = true;
        check(!f.Details()[DockyardAction.Rent].Enabled, "Restoring vessels stay unavailable through the dropdown."); cutter.IsInitializing = false;
        cutter.Hull = 0;
        check(!f.Details()[DockyardAction.Rent].Enabled, "Disabled cutter hulls cannot be rented."); cutter.Hull = 75;
        cutter.Cargo.Add(new MerchantCargo { ItemId = "grain", Quantity = 1 });
        check(!f.Details()[DockyardAction.Rent].Enabled, "Rental checkout preserves the empty-cargo prerequisite."); cutter.Cargo.Clear();
        var lease = f.Rent(0); f.Refresh(); s.ChangeMode((int)DockyardMode.ActiveRental);
        var active = f.Details();
        check(active[DockyardAction.RetrieveRental] is { Visible: true, Enabled: true } && active[DockyardAction.ReturnRental] is { Visible: true, Enabled: true } &&
            active[DockyardAction.Market].Enabled && !active[DockyardAction.Rent].Visible && active.Lines[8] == "Agreement: Active / STORED" && active.Lines[9].Contains("RETURN ELIGIBLE"),
            "An owned stored agreement shows independent retrieval, market, return actions and current return eligibility.");
        cutter.Cargo.Add(new MerchantCargo { ItemId = "grain", Quantity = 2 });
        check(!f.Details()[DockyardAction.ReturnRental].Enabled && f.Details()[DockyardAction.ReturnRental].Reason.Contains("Unload cargo") &&
            f.Details()[DockyardAction.Market].Enabled, "Loaded rentals may use the market but cannot settle their agreement."); cutter.Cargo.Clear();
        check(!f.Details(occupants: true)[DockyardAction.ReturnRental].Enabled && f.Details(occupants: true)[DockyardAction.ReturnRental].Reason.Contains("disembark"),
            "Occupied rentals retain an explicit return prohibition.");
        check(!f.Details(agreementCurrent: false)[DockyardAction.ReturnRental].Enabled && !f.Details(agreementCurrent: false)[DockyardAction.RetrieveRental].Enabled,
            "Stale ledger agreements cannot be retrieved or returned.");
        lease.Guests.Add("guest"); lease.RenterAccount = "guest";
        check(!f.Details()[DockyardAction.ReturnRental].Enabled && !f.Details()[DockyardAction.RetrieveRental].Enabled,
            "Changing the owner invalidates selection authorization; guest/party membership does not grant rental actions."); lease.RenterAccount = "owner";
        var currentLease = new ShipRentalRecord { RentalId = lease.RentalId, ShipId = lease.ShipId, Status = ShipRentalStatus.Active, Fee = 321, Deposit = 456, GoldOwed = 17 };
        check(f.Details(current: currentLease).Lines[7].Contains("321 gp") && f.Details(current: currentLease).Lines[7].Contains("17 gp"),
            "A freshly resolved ledger snapshot updates current agreement terms and refunds in the selected panel.");
        f.Storage.RetrieveShip(cutter, "southport");
        check(!f.Details()[DockyardAction.RetrieveRental].Enabled && !f.Details()[DockyardAction.ReturnRental].Enabled && f.Details().Lines[5] == "Location: ocean_01",
            "At-sea rentals expose their location and remain ineligible for stored retrieval/return.");
        f.Storage.StoreShip(cutter, "southport");
        string before = System.Text.Json.JsonSerializer.Serialize(f.Registry.GetShips().Select(ship => ShipSnapshotCodec.Capture(ship)).ToArray());
        string leasesBefore = System.Text.Json.JsonSerializer.Serialize(f.Agreements);
        foreach (var mode in Enum.GetValues<DockyardMode>()) { s.ChangeMode((int)mode); f.Details(); f.Refresh(); }
        check(before == System.Text.Json.JsonSerializer.Serialize(f.Registry.GetShips().Select(ship => ShipSnapshotCodec.Capture(ship)).ToArray()) &&
            leasesBefore == System.Text.Json.JsonSerializer.Serialize(f.Agreements), "Publishing dropdown information never mutates cargo, navigation, rental agreements or persisted ship snapshots.");
    }
    private static void Geometry(Action<bool, string> check)
    {
        var services = Services();
        try
        {
            var service = (ShipRetrievalNuiService)StartupChecks.CreateResolver(services)(typeof(ShipRetrievalNuiService));
            var f = new Fleet(); f.Rent(0); f.Refresh();
            var empty = new DockyardSelectionState("southport", "owner");
            empty.Refresh(new ShipRegistryService(), DockyardShipList.Create(new ShipRegistryService(), new ShipStorageService(), "southport", _ => true), [], []);
            foreach (int fleetSize in new[] { 0, 1, 20 })
            {
                if (fleetSize > 1) for (int i = 1; i < fleetSize; i++) f.Add("extra-" + i, "Extra stored " + i);
                f.Refresh();
                var state = fleetSize == 0 ? empty : f.State;
                foreach (var mode in Enum.GetValues<DockyardMode>())
                {
                    state.ChangeMode((int)mode);
                    var root = Layout(service, state);
                    var window = (NuiWindow)typeof(ShipRetrievalNuiService).GetMethod("CreateWindow", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [root])!;
                    var rect = ((NuiValue<NuiRect>)window.Geometry).Value;
                    var elements = Elements(window.Root).ToArray();
                    check(rect.X == -1 && rect.Y == -1 && rect.Width == 740 && rect.Height == 814 && window.Resizable is NuiValue<bool> { Value: false },
                        "Every mode and fleet size uses the same centered 740 x 814 window suitable for a 1080p client.");
                    check(root.Width == 680 && root.Height == 754 && root.Children.Sum(c => c.Height ?? 0) == root.Height &&
                        root.Children.All(c => c.Width <= root.Width && c.Margin == 0) && window.Root is NuiGroup { Border: false, Scrollbars: NuiScrollbars.None, Margin: 0, Padding: 0 },
                        "The explicitly bounded root fits all content without depending on list count or scrollable ancestors.");
                    check(!elements.Any(e => e is NuiList or NuiText or NuiTextEdit) && elements.OfType<NuiGroup>().All(g => g.Scrollbars == NuiScrollbars.None),
                        "No list templates, nested scrollbars or short scrollable text widgets survive in any mode.");
                    var combos = elements.OfType<NuiCombo>().ToArray();
                    check(combos.Length == 2 && combos.All(c => c.Width == 680 && c.Height == 40 && c.Margin == 0),
                        "Both selectors have full-width, explicit 40-pixel click areas.");
                    var labels = elements.OfType<NuiLabel>().Where(l => l.Label is NuiBind<string> b && b.Key.StartsWith("dockyard_detail_")).ToArray();
                    check(labels.Select(l => ((NuiBind<string>)l.Label).Key).SequenceEqual(new[] { "name", "class", "hull", "cargo", "weapons", "location", "availability", "terms", "agreement", "return" }
                        .Select(k => "dockyard_detail_" + k)) && labels.All(l => l.Width == 680 && l.Height == 26 && l.Tooltip != null && l.VerticalAlign is NuiValue<NuiVAlign> { Value: NuiVAlign.Middle }),
                        "The selected vessel panel has ten readable, non-scrolling bound detail lines in a fixed order.");
                    var actions = root.Children.OfType<NuiGroup>().Single(g => g.Id == "dockyard_actions");
                    var actionColumn = (NuiColumn)actions.Element!; var actionButtons = actionColumn.Children.OfType<NuiButton>().ToArray();
                    check(actions.Height == 132 && actionColumn.Height == actionColumn.Children.Sum(c => c.Height ?? 0) && actionColumn.Height <= actions.Height &&
                        actionButtons.Length == (mode == DockyardMode.ActiveRental ? 2 : 1) && actionButtons.All(b => b.Width == 680 && b.Height == 40 && b.Enabled is NuiBind<bool>),
                        "Primary and rental-return buttons sit in a dedicated non-scrolling action group, fully within its bounds.");
                    string primary = mode switch { DockyardMode.Stored => "RETRIEVE SHIP", DockyardMode.Rent => "RENT SHIP", _ => "RETRIEVE RENTAL" };
                    check(((NuiValue<string>)actionButtons[0].Label).Value == primary &&
                        actionButtons[0].Id!.EndsWith(":" + state.SelectedValue) && actionColumn.Children.OfType<NuiLabel>().All(l => l.Height == 26),
                        "The correct mode action has the captured selection ID and a dedicated readable disabled-reason label.");
                    var footer = (NuiRow)root.Children.OfType<NuiGroup>().Single(g => g.Id == "dockyard_footer").Element!;
                    check(footer.Width == 680 && footer.Height == 44 && footer.Children.Sum(c => c.Width ?? 0) == 680 &&
                        footer.Children.OfType<NuiButton>().All(b => b.Height == 40 && b.Width >= 160) &&
                        footer.Children.OfType<NuiButton>().First().Id == "shipyard_refresh" && footer.Children.OfType<NuiButton>().Last().Id == "ship_retrieval_close",
                        "REFRESH/CLOSE and optional market remain visible in the fixed footer without horizontal overflow.");
                    check(footer.Children.OfType<NuiButton>().Any(b => ((NuiValue<string>)b.Label).Value == "CARGO / MARKET") == (mode != DockyardMode.Rent),
                        "Only stored and agreement modes render their existing cargo/market action.");
                    float top = 0;
                    foreach (var child in root.Children)
                    {
                        if (child is NuiGroup)
                            check(top + child.Height <= root.Height && top + child.Height + 60 <= rect.Height,
                                "All action groups and the footer fit entirely above the window bottom.");
                        top += child.Height ?? 0;
                    }
                    var reopened = Layout(service, state);
                    check(!ReferenceEquals(root, reopened) && !ReferenceEquals(combos[0], Elements(reopened).OfType<NuiCombo>().First()) &&
                        Elements(root).OfType<NuiButton>().Select(b => b.Id).SequenceEqual(Elements(reopened).OfType<NuiButton>().Select(b => b.Id)),
                        "Reopening builds fresh elements with consistent action IDs and binding definitions.");
                }
            }
        }
        finally { foreach (var service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
    }
    private static void EventContract(Action<bool, string> check, [System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        string dir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, ".."));
        string source = File.ReadAllText(Path.Combine(dir, "ShipRetrievalNuiService.cs"));
        check(source.Contains("view.Token.Token != evt.Token.Token") && source.Contains("view.Selection.Closed") && source.Contains("view.Busy || view.Publishing") &&
            source.Contains("view.Token.GetBindValue(_modeSelection) != (int)view.Selection.Mode") && source.Contains("view.Token.GetBindValue(_shipSelection) != view.Selection.SelectedValue") &&
            source.Contains("TryAction(evt.ElementId, view.Selection") && !source.Contains("evt.ArrayIndex"),
            "Native routing checks token, busy state, current mode/value and encoded subject before acting; no array ordinal is used.");
        check(source.Contains("view.Selection.TryResolve(_registry, out var ship)") && source.Contains("!details[action].Visible || !details[action].Enabled") &&
            source.Contains("a.RentalId == state.Selected!.RentalId") && source.Contains("a.RenterAccount == state.Account") && source.Contains("bool offerCurrent"),
            "Actions re-resolve the canonical ship, current agreement/configuration, storage and authorization before transaction dispatch.");
        check(source.Contains("_partyTravel.OpenCheckout(player, ship!, view.Port)") && source.Contains("_partyTravel.OpenRetrieval(player, ship!, view.Port)") &&
            source.Contains("_rentals.ReturnAsync(player, lease.ShipId, view.Port)") && source.Contains("_trade.OpenPort(player, ship!)"),
            "Checkout, both retrieval modes, return and market dispatch the selected canonical subject to the existing backend services.");
        string watch = source[source.IndexOf("if (evt.EventType == NuiEventType.Watch)", StringComparison.Ordinal)..source.IndexOf("if (evt.EventType != NuiEventType.Click)", StringComparison.Ordinal)];
        check(watch.Contains("ChangeMode(value); Publish(player, view)") && watch.Contains("view.Selection.Select(value)") && watch.Contains("Publish(player, view)") &&
            !watch.Contains("TryCreateNuiWindow") && source.Contains("SetGroupLayout(view.Actions, CreateActions(state))") && source.Contains("SetGroupLayout(view.Footer, CreateFooter(state))"),
            "Selector watches update binds and only the action/footer sublayouts, preserving the live window token.");
        check(source.Contains("SetBindWatch(_modeSelection, false)") && source.Contains("SetBindWatch(_shipSelection, false)") &&
            source.Contains("SetBindWatch(_modeSelection, true)") && source.Contains("SetBindWatch(_shipSelection, true)"),
            "Programmatic publishing suspends and restores dropdown watches to prevent recursive selection events.");
        check(source.Contains("HandleRelatedViewChanged") && source.Contains("_partyTravel.IsTravelPending(player.PlayerName)") && source.Contains("Refresh(player, view)") &&
            source.Contains("_trade.ViewChanged += HandleRelatedViewChanged") && source.Contains("_partyTravel.TravelFinished += HandleRelatedViewChanged"),
            "Async confirmation/trade completion refreshes dockyard details, with duplicate actions disabled while boarding is pending.");
        string close = source[source.IndexOf("public void Close(NwPlayer player)", StringComparison.Ordinal)..];
        check(close.Contains("player.OnNuiEvent -= HandleEvent") && close.Contains("_windows.Remove(player.PlayerName") &&
            close.Contains("view.Selection.Close(); SailingWindowLifecycle.Close(view.Token)") && source.Contains("player.OnNuiEvent -= HandleEvent; player.OnNuiEvent += HandleEvent;") &&
            close.Contains("_trade.ViewChanged -= HandleRelatedViewChanged") && close.Contains("_partyTravel.TravelFinished -= HandleRelatedViewChanged") &&
            source.Contains("\"ship_retrieval_window\""), "Close/dispose detach event observers, retire the mapping before native close and preserve the window ID on reopen.");
        check(source.Contains("DockyardShipList.Create") && source.Contains("_rentals.Definitions, _rentals.ForAccount(state.Account)") &&
            !source.Contains("Southport Cutter 01") && !source.Contains("Sea Sprite"), "Dropdown options come from domain queries without hardcoded fleet names.");
        string trade = File.ReadAllText(Path.Combine(dir, "ShipTradeNuiService.cs"));
        string party = File.ReadAllText(Path.Combine(dir, "ShipPartyTransitionService.cs"));
        check(trade.Contains("NotifyViewChanged(player)") && trade.Contains("ViewChanged.GetInvocationList()") && party.Contains("TravelFinished.GetInvocationList()") &&
            party.Contains("IsTravelPending(string playerName)"), "Presentation observers notify after trade/travel and isolate callback failures from transactions.");
    }
}

static class PartyAndTradeChecks
{
    sealed class Engine(string area)
    {
        public string Area = area;
        public bool Fail;
        public int Jumps;
        string? _queued;
        TaskCompletionSource _frame = new();
        public void Advance()
        { if (_queued != null) { Area = _queued; _queued = null; } var frame = _frame; _frame = new(); frame.SetResult(); }
        public async Task Move(string destination, System.Action validate, bool rollback = false)
        {
            Jumps++; _queued = destination;
            await ShipTransferTransaction.AwaitArrivalAsync(() => _frame.Task, () => Area == destination,
                () => { validate(); if (Fail && !rollback) throw new InvalidOperationException("Engine failure."); });
        }
        public void Require(string area) { if (Area != area) throw new InvalidOperationException("No arrival."); }
    }
    static PartyTravelConsent Consent(DateTime now, string? exclusion = null) => new("leader",
        [new("guest", "Nearby guest", exclusion)], now.AddSeconds(60));
    public static async Task Run(Action<bool, string> check)
    {
        VerifyEligibility(check);
        DateTime now = DateTime.UtcNow;
        var solo = Consent(now);
        check(solo.TryStart(false, now, _ => true, out var soloPlayers) && soloPlayers.SequenceEqual(["leader"]),
            "BOARD ALONE requires only its initiator's authorization; pending party members are not moved.");
        var party = Consent(now);
        check(!party.TryStart(true, now, _ => true, out _) && party.Answer("guest", true, now) &&
            party.TryStart(true, now, _ => true, out var participants) && participants.SequenceEqual(["leader", "guest"]),
            "BOARD WITH PARTY waits for explicit consent, then selects two eligible players.");
        check(!party.TryStart(true, now, _ => true, out _) && !party.Answer("guest", false, now),
            "A confirmed party request cannot start or change its answers twice.");
        var declined = Consent(now);
        check(declined.Answer("guest", false, now) && declined.TryStart(true, now, _ => true, out var declinedPlayers) &&
            declinedPlayers.SequenceEqual(["leader"]), "A declining member is excluded without preventing the initiator's travel.");
        var remote = Consent(now, "Outside boarding range");
        check(!remote.Answer("guest", true, now) && remote.TryStart(true, now, _ => true, out var localOnly) && localOnly.Length == 1,
            "Ineligible remote members cannot forge an acceptance or join a transition.");
        var stale = Consent(now); stale.Answer("guest", true, now);
        check(!stale.TryStart(true, now, name => name != "guest", out _), "Accepted members leaving the party or departure invalidate confirmation.");
        var expired = Consent(now);
        check(!expired.Answer("guest", true, now.AddSeconds(60)) && !expired.TryStart(false, now.AddSeconds(60), _ => true, out _),
            "Expired confirmations cannot authorize travel.");
        var cancelled = Consent(now); cancelled.Cancel();
        check(!cancelled.TryStart(false, now, _ => true, out _), "Cancelled confirmations never move anyone.");
        using (var reservation = new ShipTransferReservations().TryReserve(["leader", "guest"])) check(reservation != null, "A whole eligible group can be reserved.");
        var reservations = new ShipTransferReservations();
        using (var held = reservations.TryReserve(["leader", "guest"]))
            check(held != null && reservations.TryReserve(["guest", "other"]) == null && !reservations.Contains("other"),
                "Overlapping party requests fail atomically without reserving unrelated players.");
        check(!reservations.Contains("leader") && !reservations.Contains("guest"), "Group reservations release after the entire transaction.");
        await VerifyTransfers(check);
        VerifyTradeRows(check);
        VerifyTradeLayout(check);
    }
    static void VerifyEligibility(Action<bool, string> check)
    {
        var anchor = new System.Numerics.Vector3(75, 45, 0);
        var eligible = new PartyTravelEligibility(true, true, true, "cordor_west", anchor, null, true, true, false);
        string? Board(PartyTravelEligibility p) => ShipPartyTravelRules.Exclusion(p, null, "cordor_west", anchor, 12, false);
        check(Board(eligible) == null && Board(eligible with { Area = "unrelated" }) == "Outside boarding range" &&
            Board(eligible with { Position = anchor + new System.Numerics.Vector3(13, 0, 0) }) == "Outside boarding range",
            "Boarding excludes party members in unrelated areas and beyond the interaction radius.");
        check(Board(eligible with { Online = false }) == "Offline" && Board(eligible with { LoginCharacter = false }) != null &&
            Board(eligible with { InParty = false }) != null && Board(eligible with { Busy = true }) != null,
            "Offline players, possessed creatures, stale party membership and concurrent transfers are ineligible.");
        check(Board(eligible with { Position = new(float.NaN, 45, 0) }) != null, "Non-finite boarding positions are rejected.");
        var rental = new ShipState { ShipId = "rental", ShipName = "Rental", DeckAreaResRef = "rental_deck", AreaResRef = "ocean_01",
            RentalPoolId = "southport", Rental = new() { RenterAccount = "owner", Status = ShipRentalStatus.Active } };
        check(Board(eligible with { Authorized = ShipAccessService.MayBoard(rental, "guest") }) == "Rental guest authorization required",
            "Party membership alone cannot authorize a rental guest.");
        rental.Rental.Guests.Add("guest"); var guests = rental.Rental.Guests.ToArray();
        check(Board(eligible with { Authorized = ShipAccessService.MayBoard(rental, "guest") }) == null &&
            !ShipAccessService.MayCommand(rental, "guest") && rental.Rental.Guests.SetEquals(guests),
            "An explicitly authorized rental guest can travel without acquiring command rights or new persistent access.");
        string? Exit(PartyTravelEligibility p) => ShipPartyTravelRules.Exclusion(p, "Source", "deck", anchor, 12, true);
        check(Exit(eligible with { Aboard = "Source", Area = "cabin", Present = true }) == null &&
            Exit(eligible with { Aboard = "Other", Present = true }) != null && Exit(eligible with { Aboard = "Source", Present = false }) != null,
            "Disembarking includes physically present members on the same ship's deck/cabin and excludes other vessels.");
    }
    static async Task VerifyTransfers(Action<bool, string> check)
    {
        var source = new ShipState { ShipId = "Source", ShipName = "Source", DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = 90, Y = 100,
            HelmsmanPCKey = "captain", Underway = true, IsNavigating = true };
        var target = new ShipState { ShipId = "Target", ShipName = "Target", DeckAreaResRef = "target_deck", AreaResRef = "ocean_01", X = 92, Y = 100,
            HelmsmanPCKey = "target-captain" };
        var aboard = new Dictionary<string, HashSet<string>> { ["Source"] = ["captain", "guest", "unrelated"], ["Target"] = ["target-captain"] };
        void Commit(string name, string? from, string? to)
        { ShipCrewLifecycle.TransferMembership(aboard, from, to, name); if (from == "Source") ShipCrewLifecycle.ReleaseOwnHelm(source, name); }
        var solo = new Engine("cordor_west");
        var soloMove = ShipTransferTransaction.ExecuteAsync([
            (() => solo.Move("target_deck", () => { }), () => solo.Move("cordor_west", () => { }, true), () => solo.Require("target_deck"))],
            () => Commit("solo", null, "Target"));
        check(!soloMove.IsCompleted && !aboard["Target"].Contains("solo"), "Solo boarding waits for an actual engine arrival.");
        solo.Advance(); check(await soloMove && aboard["Target"].Contains("solo") && target.HelmsmanPCKey == "target-captain",
            "Solo arrival commits passenger membership without taking another player's helm.");
        var a = new Engine("cordor_west"); var b = new Engine("cordor_west"); int commits = 0;
        var board = ShipTransferTransaction.ExecuteAsync([
            (() => a.Move("target_deck", () => { }), () => a.Move("cordor_west", () => { }, true), () => a.Require("target_deck")),
            (() => b.Move("target_deck", () => { }), () => b.Move("cordor_west", () => { }, true), () => b.Require("target_deck"))],
            () => { Commit("leader", null, "Target"); Commit("companion", null, "Target"); commits++; });
        a.Advance(); check(!board.IsCompleted && commits == 0, "The first arrival cannot publish a partial party boarding.");
        b.Advance(); check(await board && commits == 1 && aboard["Target"].IsSupersetOf(["leader", "companion", "target-captain"]),
            "Both eligible consenting arrivals commit together and preserve existing occupants.");
        var landfall = ShipDisembarkRules.Resolve(source, new IslandService().GetIslands(), new SailingAreaService().ContainsArea)!;
        check(landfall is { AreaResRef: "cordor_west", X: 75, Y: 45, Z: 0 } && new IslandService().GetIslands().Single(p => p.Id == "southport") is
            { OceanArea: "ocean_01", OceanX: 90, OceanY: 100, DockRadius: 10 }, "Party exit captures the verified Southport landfall rather than ocean coordinates.");
        var captain = new Engine("deck"); var guest = new Engine("cabin");
        void PortValid() { if (!ShipDisembarkRules.StillEligible(source, landfall, new IslandService().GetIslands(), new SailingAreaService().ContainsArea)) throw new InvalidOperationException("Port lost."); }
        var exit = ShipTransferTransaction.ExecuteAsync([
            (() => captain.Move(landfall.AreaResRef, PortValid), () => captain.Move("deck", () => { }, true), () => { PortValid(); captain.Require(landfall.AreaResRef); }),
            (() => guest.Move(landfall.AreaResRef, PortValid), () => guest.Move("cabin", () => { }, true), () => { PortValid(); guest.Require(landfall.AreaResRef); })],
            () => { Commit("captain", "Source", null); Commit("guest", "Source", null); });
        captain.Advance(); check(source.HelmsmanPCKey == "captain", "Captain helm remains owned until the entire exit confirms.");
        guest.Advance(); check(await exit && source.HelmsmanPCKey == null && aboard["Source"].SetEquals(["unrelated"]) && source.Underway && source.IsNavigating,
            "Party exit releases the departing captain's helm and preserves unrelated crew and navigation.");
        source.X = 101;
        check(ShipDisembarkRules.Resolve(source, new IslandService().GetIslands(), new SailingAreaService().ContainsArea) == null,
            "Party exit outside Southport's docking radius is rejected."); source.X = 90;
        var book = new BoardingRequestBook(); DateTime now = DateTime.UtcNow;
        var request = new ShipBoardingRequest { RequestingShip = source, TargetShip = target, RequestingPlayerName = "leader", TargetPlayerName = "target-captain", RequestedAt = now };
        check(book.Add(request, now) && book.ForTarget("other-captain") == null && book.ForTarget("target-captain")!.TargetShip == target,
            "Ship-to-ship party travel retains the explicitly selected target and correct captain's boarding consent.");
        var accepted = new AcceptedBoardingTravelBook();
        check(!accepted.IsValid(request, now) && !accepted.TryStart(request, now), "A pending boarding request cannot authorize party travel before captain acceptance.");
        accepted.Record(request, now.AddSeconds(60));
        check(accepted.IsValid(request, now), "Captain acceptance authorizes the exact captured boarding request.");
        var forged = new ShipBoardingRequest { RequestingShip = source, TargetShip = target, RequestingPlayerName = "leader", TargetPlayerName = "target-captain", RequestedAt = now };
        check(!accepted.IsValid(forged, now), "Copying request fields cannot forge the target captain's accepted request.");
        request.TargetPlayerName = "other-captain";
        check(!accepted.IsValid(request, now), "Changing the accepted target or captain invalidates authorization."); request.TargetPlayerName = "target-captain";
        check(accepted.TryStart(request, now) && !accepted.TryStart(request, now), "Captain acceptance starts only one transfer, including duplicate clicks.");
        check(accepted.IsValid(request, now.AddSeconds(61)), "An already started frame-awaited transfer retains its authorization until completion.");
        accepted.Remove(request);
        check(!accepted.IsValid(request, now) && !accepted.TryStart(request, now), "Finished or cancelled captain consent cannot be reused.");
        accepted.Record(request, now.AddSeconds(60));
        check(!accepted.TryStart(request, now.AddSeconds(60)), "Unconfirmed captain consent expires without moving anyone.");
        accepted.CancelPlayer("leader");
        check(!accepted.IsValid(request, now), "A disconnect or departure invalidates accepted ship-to-ship travel.");
        accepted.Record(request, now.AddSeconds(60)); accepted.CancelShip(target);
        check(!accepted.IsValid(request, now), "Removing the selected ship invalidates its captain's acceptance.");
        accepted.Record(request, now.AddSeconds(60)); accepted.Cleanup(now, _ => false);
        check(!accepted.IsValid(request, now), "Loss of range or helm ownership invalidates accepted travel.");
        var failA = new Engine("shore"); var failB = new Engine("shore") { Fail = true }; int failedCommits = 0;
        var failed = ShipTransferTransaction.ExecuteAsync([
            (() => failA.Move("deck", () => { }), () => failA.Move("shore", () => { }, true), () => failA.Require("deck")),
            (() => failB.Move("deck", () => { }), () => failB.Move("shore", () => { }, true), () => failB.Require("deck"))], () => failedCommits++);
        failA.Advance(); failB.Advance();
        check(!failed.IsCompleted && failedCommits == 0, "Second-participant engine failure begins frame-awaited rollback without membership changes.");
        failB.Advance(); failA.Advance();
        check(!await failed && failA.Area == "shore" && failB.Area == "shore" && failedCommits == 0,
            "Transfer failure rolls every attempted participant back and commits no group memberships.");
    }
    static void VerifyTradeRows(Action<bool, string> check)
    {
        var options = new MaritimeOptions();
        var economy = new MaritimeEconomyState { Markets = options.Markets.ToDictionary(m => m.PortId, MaritimeMarketEngine.Copy) };
        var source = new ShipState { ShipId = "Trader", ShipName = "Trader", DeckAreaResRef = "deck", AreaResRef = "ocean_01", CargoCapacity = 50 };
        var merchant = new ShipState { ShipId = "Merchant", ShipName = "Merchant", DeckAreaResRef = "merchant_deck", AreaResRef = "ocean_01",
            ShipType = ShipType.Merchant, IsInPort = true, CurrentTradePortId = "southport", CargoCapacity = 100, MerchantGold = 5000,
            Cargo = [new() { ItemId = "grain", Quantity = 10 }] };
        var registry = new ShipRegistryService(); registry.Register(source); registry.Register(merchant);
        var view = new ShipTradeViewState(source, null, "southport"); view.Refresh(economy);
        check(view.Buying.Select(r => r.Item).SequenceEqual(["grain", "timber"]) && view.Selling.Count == 0 && view.Remaining == 50,
            "Market population exposes all configured commodities and an independently empty cargo table.");
        view.CaptureQuantities(true, ["20", "3"]);
        check(view.TryResolve(true, 0, 1000, out string item, out int quantity) && item == "grain" && quantity == 20,
            "The buy action reads its selected row's quantity rather than a global or sell quantity.");
        var vessel = MaritimeMarketEngine.Capture(source); economy.Vessels[source.ShipId] = vessel;
        long gold = 5000; int stock = economy.Markets["southport"].Goods[item].Stock;
        gold += MaritimeMarketEngine.PortTrade(economy, vessel, source.CargoCapacity, "southport", item, quantity, true, gold, DateTime.UtcNow);
        MaritimeMarketEngine.Apply(source, vessel); view.Refresh(economy);
        check(view.Used == 20 && view.Remaining == 30 && view.Selling.Single().Aboard == 20 && view.Buying[0].Stock == stock - 20 && gold == 4840,
            "Buying twenty units refreshes aboard cargo, capacity, gold and market stock consistently.");
        view.CaptureQuantities(false, ["7"]);
        check(view.TryResolve(false, 0, 1000, out item, out quantity) && quantity == 7 && view.TryResolve(true, 0, 1000, out _, out int buyQuantity) && buyQuantity == 20,
            "Independent buy and sell quantity arrays remain bound to their commodity rows after refresh.");
        gold += MaritimeMarketEngine.PortTrade(economy, vessel, 50, "southport", item, quantity, false, gold, DateTime.UtcNow);
        MaritimeMarketEngine.Apply(source, vessel); view.Refresh(economy);
        check(view.Used == 13 && view.Remaining == 37 && view.Selling.Single().Aboard == 13 && view.Buying[0].Stock == stock - 13 && gold > 4840,
            "Selling seven units refreshes cargo space, payment and market stock without closing the view.");
        source.Cargo.Clear(); source.Cargo.Add(new() { ItemId = "timber", Quantity = 2 }); view.Refresh(economy);
        check(view.Selling.Select(r => r.Item).SequenceEqual(["grain", "timber"]) && view.Selling[0].Aboard == 0 && view.Selling[1].Aboard == 2 &&
            view.TryResolve(false, 0, 1000, out item, out _) && item == "grain", "Sold-out rows retain their identity; a newly loaded commodity cannot reuse a delayed sale's row.");
        foreach (string invalid in new[] { "0", "-1", "1.5", "1001", "garbage", "99999999999999" })
        { view.CaptureQuantities(false, [invalid]); check(!view.TryResolve(false, 0, 1000, out _, out _), "Invalid, negative, overflow and excessive quantities are rejected."); }
        check(!view.TryResolve(true, -1, 1000, out _, out _) && !view.TryResolve(true, 99, 1000, out _, out _), "Invalid trade array indices address no commodity.");
        var captured = new ShipTradeViewState(source, merchant, "southport"); captured.Refresh(economy);
        check(captured.Buying[0].Stock == 10 && captured.Buying[0].MarketStock == stock - 13 && captured.Merchant == merchant && captured.Port == "southport",
            "Merchant stock is distinguished from port stock while merchant and market identities remain captured.");
        merchant.X = 1000;
        check(!ShipTradeNuiService.IsEligible(new(source, merchant, "southport")) && captured.Merchant == merchant,
            "An out-of-range captured merchant is rejected without falling back to a nearby merchant.");
        merchant.X = 0; merchant.IsInPort = false;
        check(!ShipTradeNuiService.IsEligible(new(source, merchant, "southport")), "A merchant departing its market invalidates the old trade target.");
        registry.Remove(merchant);
        check(!captured.IsCurrent(registry), "Registry removal invalidates a captured merchant's instance.");
        view.Close(); check(!view.TryResolve(true, 0, 1000, out _, out _), "A closed window cannot execute a delayed trade event.");
        var reopened = new ShipTradeViewState(source, null, "southport"); reopened.Refresh(economy);
        check(reopened.Selling.Single().Item == "timber" && reopened.Quantities(false).Single() == "1" && reopened.Source == source,
            "Reopening creates a fresh cargo snapshot and quantity defaults without old exhausted row identities.");
    }
    static void VerifyTradeLayout(Action<bool, string> check)
    {
        Dictionary<Type, object> services = new() { [typeof(EventService)] = new EventService([]),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
            [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options) };
        try
        {
            var service = (ShipTradeNuiService)StartupChecks.CreateResolver(services)(typeof(ShipTradeNuiService));
            var state = new ShipTradeViewState(new() { ShipName = "Trade test", DeckAreaResRef = "deck", AreaResRef = "ocean_01" }, null, "southport");
            var layout = (NuiColumn)typeof(ShipTradeNuiService).GetMethod("CreateLayout", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, [state])!;
            var lists = layout.Children.OfType<NuiList>().ToArray();
            check(lists.Length == 2 && lists[0].RowCount != lists[1].RowCount && lists.All(l => l.Height > 2 * l.RowHeight && l.RowHeight >= 48),
                "Actual trade layout has two independent scrollable lists with consistent readable row heights.");
            check(lists.Select(l => l.RowTemplate[4].Element.Id).SequenceEqual(["maritime_buy_qty", "maritime_sell_qty"]) &&
                lists.Select(l => l.RowTemplate[5].Element.Id).SequenceEqual(["maritime_buy", "maritime_sell"]),
                "Actual table templates bind separate per-row buy/sell quantities and stable event IDs.");
            var quantityBinds = lists.Select(l => l.RowTemplate[4].Element).Select(e =>
                e.GetType().GetProperties().Select(p => p.GetValue(e)).OfType<NuiBind<string>>().Single().Key).ToArray();
            check(quantityBinds.SequenceEqual(["maritime_buy_quantity", "maritime_sell_quantity"]), "Actual NUI text edits use independent buy/sell quantity binds.");
            var window = SailingNuiLayout.Window(layout, "Trade", 1010, 870);
            var rect = ((NuiValue<NuiRect>)window.Geometry).Value;
            check(rect.X == -1 && rect.Y == -1 && ((NuiValue<bool>)window.Resizable!).Value == false &&
                window.Root is NuiGroup { Scrollbars: NuiScrollbars.Auto }, "Trade window geometry centers at a readable fixed size with an outer scrolling fallback.");
        }
        finally { foreach (var service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
    }
}
#endif

#if SAILING_REGRESSION
// Recovery checks inspect the actual drawing surface, window root and scroll ancestry.
static class NauticalChartRecoveryChecks
{
    static T Value<T>(NuiProperty<T> property) => ((NuiValue<T>)property).Value!;
    static ShipState Ship(string id, float x = 90, float y = 100) => new()
    { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = x, Y = y, SpritePrefix = "sloop" };
    static ShipNavigationService Navigation() => new(new ShipObstacleService(), new SailingAreaService());
    static NauticalChartFrame Frame(ShipState ship, ShipNavigationService navigation, string? cells = null,
        ShipState[]? fleet = null, string? selected = null) => NauticalChartProjection.Capture(ship, navigation,
        new ShipVisibilityService().GetVisibleShips(ship, fleet ?? [ship]), [], [], [], new IslandService().GetIslands(),
        (x, y) => cells == null || cells[x * 16 + y] == '1', selected);
    static bool Near(float a, float b) => MathF.Abs(a - b) < .0001f;
    static bool Center(ChartRect rect, ChartPoint point) => Near(rect.X + rect.Width / 2, point.X) && Near(rect.Y + rect.Height / 2, point.Y);

    public static IEnumerable<NuiElement> Elements(NuiElement element)
    {
        yield return element;
        IEnumerable<NuiElement> children = element switch
        {
            NuiColumn column => column.Children,
            NuiRow row => row.Children,
            NuiGroup group => group.Element == null ? [] : [group.Element],
            NuiList list => list.RowTemplate.Select(cell => cell.Element),
            _ => []
        };
        foreach (var child in children) foreach (var descendant in Elements(child)) yield return descendant;
    }

    public static void Run(Action<bool, string> check)
    {
        DrawingContract(check);
        LayoutContract(check);
        TargetAndLifecycle(check);
    }

    static void DrawingContract(Action<bool, string> check)
    {
        var navigation = Navigation();
        var locations = new[] { (0f, 0f), (640f, 0f), (640f, 640f), (0f, 640f), (320f, 320f), (90f, 100f), (120f, 90f), (20f, 630f) };
        foreach (float size in new[] { 416f, 560f, 608f, 640f, 800f })
        {
            foreach (var (x, y) in locations)
            {
                var ship = Ship("own", x, y);
                var update = new NauticalChartRenderer().Prepare(Frame(ship, navigation), size);
                var canvas = update.Layout!; var background = (NuiDrawListImage)canvas.DrawList![0];
                var point = new ChartPoint(x / 640 * size, size - y / 640 * size);
                var ring = (ChartRect)update.Changes.Single(c => c.Key == "chart_m0_ring").Value;
                var image = (ChartRect)update.Changes.Single(c => c.Key == "chart_m0_image").Value;
                var dot = (ChartRect)update.Changes.Single(c => c.Key == "chart_m0_dot").Value;
                check(canvas.Width == size && canvas.Height == size && canvas.Margin == 0 && canvas.Padding == 0 && Value(canvas.Scissor!),
                    "The actual drawing owner is a zero-inset, square, scissored surface, independent of client window size.");
                check(Value(background.Rect) is { X: 0, Y: 0 } r && r.Width == size && r.Height == size &&
                    Value(background.Aspect!) == NuiAspect.Stretch && Value(background.HorizontalAlign!) == NuiHAlign.Left && Value(background.VerticalAlign!) == NuiVAlign.Top,
                    "Full map artwork is explicitly square and top-left aligned to the same surface as the overlays.");
                check(Center(ring, point) && Center(image, point) && Center(dot, point),
                    "Actual own-ship sprite/ring/dot binds retain the world anchor, including all four corners, without inward position shifts.");
                var sprite = canvas.DrawList.OfType<NuiDrawListImage>().Single(i => i.ResRef is NuiBind<string>);
                check(Value(sprite.Aspect!) == NuiAspect.Fit && Value(sprite.HorizontalAlign!) == NuiHAlign.Center && Value(sprite.VerticalAlign!) == NuiVAlign.Middle,
                    "Native sprite fitting is explicitly centered on the marker rectangle, regardless of directional texture aspect.");
            }

            var own = Ship("own"); navigation.SetDestination(own, "ocean_01", 120, 90, 0);
            var routed = new NauticalChartRenderer().Prepare(Frame(own, navigation), size);
            var from = (ChartPoint)routed.Changes.Single(c => c.Key == "chart_r0_a").Value;
            var to = (ChartPoint)routed.Changes.Single(c => c.Key == "chart_r0_b").Value;
            var destination = (ChartRect)routed.Changes.Single(c => c.Key == "chart_w0_rect").Value;
            check(Near(from.X, 90f / 640 * size) && Near(from.Y, size - 100f / 640 * size) &&
                Near(to.X, 120f / 640 * size) && Near(to.Y, size - 90f / 640 * size) && Center(destination, to),
                "Route vectors and destination markers use the same real port/world coordinates as the chart and ships.");
            var fog = routed.Layout!.DrawList!.OfType<NuiDrawListPolyLine>()
                .Where(d => d.Enabled is NuiBind<bool> bind && bind.Key.StartsWith("chart_f")).ToArray();
            check(fog.Length == 256 && fog.Select(d => Value(d.Color!).AlphaF).All(a => a == 1) &&
                fog.All(d => Value(d.Fill!) && Value(d.LineThickness!) == 0) &&
                fog.Select(d => (Value(d.Color!).RedF, Value(d.Color!).GreenF, Value(d.Color!).BlueF, Value(d.Color!).AlphaF)).Distinct().Count() == 1 &&
                routed.Layout.DrawList!.OfType<NuiDrawListImage>().All(i => i.ResRef is not NuiValue<string> value || value.Value != "fog_cell"),
                "The actual mask has 256 uniformly colored opaque fills, zero borders and no decorative fog tile images.");
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
            {
                var cell = fog.Single(d => ((NuiBind<bool>)d.Enabled!).Key == $"chart_f{x}_{y}");
                float edge = size / 16, px = x * edge, py = y * edge;
                check(cell.Points.SequenceEqual(new[] { px, py, px + edge, py, px + edge, py + edge, px, py + edge, px, py }),
                    "Every actual fog polygon meets its adjacent cell exactly, covering all chart edges without offsets or geometric seams.");
            }
            var mask = Enumerable.Range(0, 256).Select(i => i % 3 == 0 ? '1' : '0').ToArray();
            var masked = new NauticalChartRenderer().Prepare(Frame(own, navigation, new(mask)), size);
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
                check(Equals(masked.Changes.Single(c => c.Key == $"chart_f{x}_{y}").Value, mask[x * 16 + y] != '1'),
                    "All 256 native fog-enable binds preserve the exact column-major discovery mask; presentation reveals no extra cells.");
            navigation.CancelNavigation(own);
        }
    }

    static void LayoutContract(Action<bool, string> check)
    {
        Dictionary<Type, object> services = new() { [typeof(EventService)] = new EventService([]),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
            [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options) };
        try
        {
            var service = (SailingNuiService)StartupChecks.CreateResolver(services)(typeof(SailingNuiService));
            foreach (float size in new[] { 416f, 560f, 608f, 640f, 800f })
            {
                var root = (NuiColumn)typeof(SailingNuiService).GetMethod("CreateLayout", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [Ship("own"), size])!;
                var window = (NuiWindow)typeof(SailingNuiService).GetMethod("CreateWindow", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [root])!;
                var content = (NuiGroup)window.Root; var body = (NuiRow)root.Children[1];
                var left = (NuiColumn)body.Children[0]; var map = (NuiGroup)left.Children[0]; var right = (NuiGroup)body.Children[2];
                var panel = (NuiColumn)right.Element!; var additional = panel.Children.OfType<NuiGroup>().Single();
                check(content.Scrollbars == NuiScrollbars.None && right.Scrollbars == NuiScrollbars.None && map.Scrollbars == NuiScrollbars.None &&
                    !content.Border && !map.Border && !right.Border && content.Margin == 0 && content.Padding == 0,
                    "Actual window root, chart and fixed control panel cannot introduce outer or horizontal scrollbars.");
                check(map.Width == size && map.Height == size && Value(map.Scissor!) && left.Width == size &&
                    body.Width == size + 332 && root.Width == body.Width && left.Height == size + 56 &&
                    left.Children.Skip(1).All(e => e is NuiLabel { Height: 28 }),
                    "The square map has its own compact legend below it and explicit non-overlapping column dimensions.");
                check(Elements(content).OfType<NuiText>().Count() == 0 &&
                    Elements(content).OfType<NuiGroup>().Count(g => g.Scrollbars != NuiScrollbars.None) == 1 &&
                    additional.Scrollbars == NuiScrollbars.Y && additional.Height >= 92 &&
                    Elements(additional).OfType<NuiGroup>().All(g => ReferenceEquals(g, additional)) && !Elements(additional).OfType<NuiList>().Any(),
                    "Only one additional-controls panel scrolls; no readout fields or nested scrolling groups/lists exist within it.");
                var target = panel.Children.OfType<NuiList>().Single();
                check(target.Id == "sailing_target_list" && target.Scrollbars == NuiScrollbars.Y && target.Width == 304 &&
                    target.Height == 96 && target.RowHeight == 44 && target.RowTemplate.Sum(c => c.Width) + 16 <= target.Width &&
                    target.RowTemplate[0].Element is NuiLabel { Width: 208, Height: 44 } &&
                    target.RowTemplate[1].Element is NuiButton { Id: "sailing_target_select", Width: 74, Height: 36 },
                    "Fixed target selection remains outside the scrolling actions, with explicit cell sizes and room for its own vertical scrollbar.");
                var essential = panel.Children.TakeWhile(e => !ReferenceEquals(e, additional)).ToArray();
                var buttons = essential.SelectMany(Elements).OfType<NuiButton>().Where(b => b.Id != "sailing_target_select").ToArray();
                check(buttons.Select(b => b.Id).Order().SequenceEqual(new[] { "ahead_button", "astern_button", "dock_button", "left_button", "right_button", "stop_button" }.Order()) &&
                    buttons.All(b => b.Width >= 98 && b.Height == 40) && essential.Sum(c => c.Height) + additional.Height == panel.Height,
                    "Every helm/docking button has an explicit clickable size and stays in the fixed portion of the panel.");
                foreach (var row in Elements(panel).OfType<NuiRow>())
                    check(row.Children.All(c => c.Width != null && c.Height != null) && row.Children.Sum(c => c.Width) <= row.Width,
                        "Every control/readout row has explicit sizes that fit its available width without horizontal scrolling.");
                check(Elements(panel).OfType<NuiLabel>().All(l => l.Height >= 28 && l.Width <= 304) &&
                    essential.OfType<NuiLabel>().Any(l => l.Label is NuiBind<string> bind && bind.Key == "chart_next") &&
                    essential.OfType<NuiLabel>().Any(l => l.Label is NuiBind<string> bind && bind.Key == "chart_final"),
                    "Navigation/route values are plain readable labels, with separate compact next and final destination fields.");
            }
        }
        finally { foreach (var service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
        check(NauticalChartGeometry.ResizedSize(100, 100) == 416 && NauticalChartGeometry.InitialSize(800, 600) == 416,
            "Undersized geometry clamps to a usable chart/control minimum that fits the common 800-by-600 client.");
        var log = NauticalChartReadouts.LogLines("First\nSecond\nThird\nFourth\nLast line", 4);
        check(log.SequenceEqual(["Second", "Third", "Fourth", "Last line"]) &&
            NauticalChartReadouts.LogLines(new string('X', 90), 4).All(l => l.Length <= 44),
            "Command feedback uses bounded plain labels for the latest lines, without nested text-field scrollbars.");
    }

    static void TargetAndLifecycle(Action<bool, string> check)
    {
        var own = Ship("own"); var a = Ship("a", 100, 100); var b = Ship("b", 110, 100); var nav = Navigation();
        var sailing = new ShipTargetSelection(); var combat = new ShipTargetSelection();
        sailing.Refresh("captain", own, [a, b]); combat.Refresh("captain", own, [a, b]);
        check(sailing.Select("captain", own, 1, [a, b]) && combat.Select("captain", own, 0, [a, b]), "Existing displayed target rows select their exact canonical ship identities.");
        var selected = sailing.GetSelected("captain", own, [a, b]); var cache = new NauticalChartWindowCache(); var first = cache.Open("captain");
        check(NauticalChartReadouts.TargetSummary(own, selected) == "b | 20.0m" &&
            NauticalChartReadouts.TargetSummary(own, null) == "Target: NONE", "A full-width plain target readout keeps the exact selected ship name and actual range.");
        var frame = Frame(own, nav, fleet: [own, a, b], selected: selected!.ShipId);
        first.Prepare(frame, 416); first.Prepare(frame, 608);
        check(ReferenceEquals(sailing.GetSelected("captain", own, [b, a]), b) && ReferenceEquals(combat.GetSelected("captain", own, [b, a]), a),
            "Layout recreation cannot reorder stored selections or mix independent Sailing and Combat targets.");
        cache.Close("captain"); var reopened = cache.Open("captain");
        check(!ReferenceEquals(first, reopened) && reopened.Prepare(frame, 608).Layout != null && reopened.Prepare(frame, 608).Changes.Count == 0,
            "Closing/reopening produces fresh drawing/bind state and then resumes unchanged-frame caching.");
        nav.SetDestination(own, "ocean_01", 120, 90, 0);
        reopened.Prepare(Frame(own, nav), 608); nav.CancelNavigation(own);
        check(!own.IsNavigating && !own.Underway && nav.GetRoute(own) == null && Frame(own, nav).Route.Length == 0,
            "The existing STOP action still cancels real navigation and removes plotted routes.");
        sailing.Refresh("captain", own, [a]);
        check(!sailing.Select("captain", own, 1, [a]) && sailing.GetSelected("captain", own, [a]) == null &&
            ReferenceEquals(combat.GetSelected("captain", own, [a]), a), "Stale target rows still fail closed and cannot replace the other window's target.");
    }
}
#endif


#if SAILING_REGRESSION
// Exercises the production projection, draw-list bindings and per-window cache without a native NWN client.
static class NauticalChartChecks
{
    static ShipState Ship(string id, float x = 320, float y = 320, ShipType type = ShipType.Player) => new()
    { ShipId = id, ShipName = id, DeckAreaResRef = "deck", AreaResRef = "ocean_01", X = x, Y = y,
        ShipType = type, SpritePrefix = type == ShipType.Merchant ? "cog" : type == ShipType.Pirate ? "brig" : "sloop" };
    static ShipNavigationService Navigation() => new(new ShipObstacleService(), new SailingAreaService());
    static NauticalChartFrame Frame(ShipState ship, ShipNavigationService nav, ChartDiscoveryService? discovery = null,
        string account = "a", ShipState[]? fleet = null, OceanContactService? ocean = null,
        ChartLandmark[]? landmarks = null, ChartMarker[]? markers = null, string? selected = null)
    {
        var cells = discovery?.CaptureCells(account, ship.AreaResRef) ?? new string('1', 256);
        return NauticalChartProjection.Capture(ship, nav, new ShipVisibilityService().GetVisibleShips(ship, fleet ?? [ship]),
            (ocean ?? new OceanContactService()).GetVisibleContacts(ship), landmarks ?? [], markers ?? [],
            new IslandService().GetIslands(), (x, y) => cells[x * 16 + y] == '1', selected);
    }
    static T Value<T>(NuiProperty<T> property) => ((NuiValue<T>)property).Value!;
    static bool Inside(ChartRect rect, float size) => rect.X >= 0 && rect.Y >= 0 && rect.Width > 0 && rect.Height > 0 &&
        rect.X + rect.Width <= size && rect.Y + rect.Height <= size;
    static string Persisted(ShipState ship) => System.Text.Json.JsonSerializer.Serialize(ShipSnapshotCodec.Capture(ship));

    public static void Run(Action<bool, string> check)
    {
        CoordinatesAndHeadings(check);
        VisibilityAndFog(check);
        RoutesAndCache(check);
        LayoutAndLifetime(check);
    }
    static void CoordinatesAndHeadings(Action<bool, string> check)
    {
        var headings = new[] { ("n", 0f, -1f), ("ne", .7071068f, -.7071068f), ("e", 1f, 0f), ("se", .7071068f, .7071068f),
            ("s", 0f, 1f), ("sw", -.7071068f, .7071068f), ("w", -1f, 0f), ("nw", -.7071068f, -.7071068f) };
        var ship = Ship("own"); var nav = Navigation();
        foreach (Heading heading in Enum.GetValues<Heading>())
        {
            ship.Heading = heading; var frame = Frame(ship, nav); var expected = headings[(int)heading];
            check(NauticalChartGeometry.ShipSprite(ship) == "sloop_" + expected.Item1 &&
                frame.Heading == "Heading: " + expected.Item1.ToUpperInvariant() &&
                NauticalChartGeometry.HeadingVector(heading) == new ChartPoint(expected.Item2, expected.Item3),
                "All eight existing sprite suffixes, cardinal instruments and north-up heading vectors agree.");
        }
        var mapping = new[] { (0f, 0f, 0f, 640f), (640f, 0f, 640f, 640f), (0f, 640f, 0f, 0f),
            (640f, 640f, 640f, 0f), (320f, 320f, 320f, 320f), (90f, 100f, 90f, 540f), (120f, 90f, 120f, 550f) };
        foreach (var point in mapping)
        {
            check(NauticalChartGeometry.Project(point.Item1, point.Item2, 640) == new ChartPoint(point.Item3, point.Item4),
                "Corners, midpoint and configured port anchors retain the actual world-to-chart transform.");
            check(NauticalChartGeometry.Project(point.Item1, point.Item2, 800) == new ChartPoint(point.Item3 * 1.25f, point.Item4 * 1.25f),
                "The former 800-pixel geographic alignment is retained when the square canvas is scaled.");
        }
        check(NauticalChartGeometry.Cell(0, 0) == (0, 15) && NauticalChartGeometry.Cell(640, 640) == (15, 0) &&
            NauticalChartGeometry.Cell(320, 320) == (8, 7), "Discovery uses the established 40-world-unit cells and inverted Y, including exact boundaries.");
        foreach (float size in new[] { 320f, 416f, 576f, 608f, 640f, 800f })
        {
            double covered = 0;
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
            {
                var cell = NauticalChartGeometry.FogCell(x, y, size); covered += cell.Width * cell.Height;
                check(Inside(cell, size) && cell.X == x * size / 16 && cell.Y == y * size / 16 &&
                    (x == 0 || NauticalChartGeometry.FogCell(x - 1, y, size).X + cell.Width == cell.X) &&
                    (y == 0 || NauticalChartGeometry.FogCell(x, y - 1, size).Y + cell.Height == cell.Y),
                    "Every fog cell tiles the full square with no horizontal offset, gap or overflow at supported sizes.");
            }
            check(covered == size * size, "All 256 fog cells cover exactly the chart's pixel area.");
        }
        foreach (var point in mapping.Take(4))
            check(Inside(NauticalChartGeometry.Marker(point.Item1, point.Item2, 40, 40, 640), 640),
                "Conservative caption-avoidance footprints are kept within each chart corner.");
        check(!NauticalChartGeometry.InWorld(-1, 0) && !NauticalChartGeometry.InWorld(641, 0) &&
            !NauticalChartGeometry.InWorld(float.NaN, 100) && NauticalChartGeometry.InWorld(640, 640),
            "Non-finite or out-of-world positions cannot construct visible markers.");
        var artwork = NauticalChartGeometry.Artwork(640, 640, 80, 40, 640);
        check(artwork == new ChartRect(608, -16, 64, 32),
            "Geographic artwork scales from the original 800-pixel footprint without moving its world anchor at an edge.");
        check(NauticalChartGeometry.ClipLine(new(-100, 320), new(1000, 320), 640, out var line) &&
            line.From == new ChartPoint(1, 320) && line.To == new ChartPoint(639, 320),
            "Route line strokes clip at the canvas boundary without altering route coordinates.");
        check(!NauticalChartGeometry.ClipLine(new(-10, -10), new(-1, -1), 640, out _) &&
            !NauticalChartGeometry.ClipLine(new(float.NaN, 10), new(20, 20), 640, out _), "Invisible and invalid route segments produce no drawing.");
        foreach (var device in new[] { (800f, 600f), (1024f, 768f), (1280f, 720f), (1920f, 1080f) })
        {
            float size = NauticalChartGeometry.InitialSize(device.Item1, device.Item2);
            check(size + NauticalChartGeometry.WindowWidthOverhead <= device.Item1 &&
                size + NauticalChartGeometry.WindowHeightOverhead <= device.Item2 && size % 16 == 0,
                "Common client resolutions get a square, integer-cell chart that fits with its instrument panel.");
        }
        check(NauticalChartGeometry.InitialSize(-1, -1) == 640 && NauticalChartGeometry.InitialSize(1920, 1080, 150) == 560,
            "Unavailable device properties use a readable fallback; GUI scale is included in advertised-screen sizing.");
    }
    static void VisibilityAndFog(Action<bool, string> check)
    {
        var own = Ship("own", 20, 620); var nav = Navigation(); var discovery = new ChartDiscoveryService();
        var cells = new string('0', 256).ToCharArray(); cells[0] = '1';
        discovery.Restore("a", [new() { AccountKey = "a", AreaResRef = "ocean_01", Cells = new(cells) }]);
        var north = new ChartLandmark { AreaResRef = "ocean_01", X = 20, Y = 620, Sprite = "", Width = 18, Height = 18 };
        var south = new ChartLandmark { AreaResRef = "ocean_01", X = 20, Y = 20, Sprite = "", Width = 18, Height = 18 };
        ShipState known = Ship("known", 30, 620, ShipType.Merchant), hidden = Ship("hidden", 60, 620, ShipType.Pirate);
        var raw = new OceanContactService();
        raw.RegisterContact(new() { Id = "known", Name = "duplicate merchant", Type = EncounterType.Merchant, AreaResRef = "ocean_01", X = 30, Y = 620 });
        raw.RegisterContact(new() { Id = "neutral", Name = "Visible wreck", Type = EncounterType.Wreck, AreaResRef = "ocean_01", X = 25, Y = 620 });
        raw.RegisterContact(new() { Id = "invisible", Name = "Hidden contact", Type = EncounterType.Pirate, AreaResRef = "ocean_01", X = 25, Y = 620, Visible = false });
        raw.RegisterContact(new() { Id = "converted", Name = "Converted contact", Type = EncounterType.Pirate, AreaResRef = "ocean_01", X = 25, Y = 620, ConvertedToShip = true });
        var frame = Frame(own, nav, discovery, fleet: [own, known, known, hidden], ocean: raw, landmarks: [north, south]);
        check(frame.Discovery[0] == '1' && frame.Discovery[15] == '0' && frame.Features.Length == 1 && frame.Features[0].Y == 620,
            "Northwest discovery reveals only the northern landmark, not its Y-mirrored undiscovered counterpart.");
        check(frame.Markers.Select(m => m.Id).SequenceEqual(["neutral", "known", "own"]) &&
            frame.Markers.Select(m => m.Id).Distinct().Count() == 3, "Ship/contact identity is deduplicated and undiscovered, invisible or converted contacts are excluded.");
        check(frame.Markers.Last().Kind == ChartContactKind.OwnShip && frame.Markers.Single(m => m.Id == "known").Kind == ChartContactKind.Merchant,
            "The own ship has foreground priority and merchant identity is retained.");
        discovery.RevealAroundShip("a", own);
        frame = Frame(own, nav, discovery, fleet: [own, known, hidden]);
        check(frame.Markers.Single(m => m.Id == "hidden").Kind == ChartContactKind.Pirate &&
            NauticalChartRenderer.MarkerColor(ChartContactKind.Pirate).RedF > NauticalChartRenderer.MarkerColor(ChartContactKind.Pirate).GreenF &&
            NauticalChartRenderer.MarkerColor(ChartContactKind.Merchant).GreenF > NauticalChartRenderer.MarkerColor(ChartContactKind.Merchant).RedF,
            "Pirate and merchant markers use their actual classifications with distinct restrained colors.");
        known.IsStored = true; hidden.AreaResRef = "ocean_002";
        check(Frame(own, nav, fleet: [own, known, hidden]).Markers.Length == 1, "Stored and cross-area ships remain excluded by actual visibility rules.");
        known.IsStored = false; known.X = 100;
        check(Frame(own, nav, fleet: [own, known]).Markers.Length == 1, "Even a discovered ship beyond the existing 70-unit horizon cannot appear.");
        own.SpritePrefix = "";
        var fallback = new NauticalChartRenderer().Prepare(Frame(own, nav), 640).Layout!;
        check(fallback.DrawList!.OfType<NuiDrawListCircle>().Any() &&
            fallback.DrawList!.OfType<NuiDrawListImage>().All(i => i.ResRef is not NuiBind<string>),
            "A missing sprite reference retains a supported primitive marker and heading indicator, without inventing an asset.");
        var empty = new ChartDiscoveryService();
        check(empty.CaptureCells("unused", "ocean_01") == new string('0', 256) && empty.Capture("unused").Count == 0,
            "Reading an unvisited chart does not create or persist discovery data.");
    }
    static void RoutesAndCache(Action<bool, string> check)
    {
        var ship = Ship("own", 90, 100); var nav = Navigation(); var renderer = new NauticalChartRenderer();
        var first = Frame(ship, nav);
        check(renderer.Prepare(first, 640).Layout != null && renderer.Prepare(first, 640).Changes.Count == 0,
            "Opening builds once; an unchanged ship, discovery and route issue no chart bind writes.");
        ship.Heading = Heading.NorthWest;
        var heading = renderer.Prepare(Frame(ship, nav), 640);
        check(heading.Layout == null && heading.Changes.Any(c => c.Key == "ship_heading") &&
            heading.Changes.Any(c => c.Key.EndsWith("_sprite")), "Heading changes update the sprite, bearing and readout without rebuilding the chart.");
        ship.X = 95;
        var moved = renderer.Prepare(Frame(ship, nav), 640);
        check(moved.Layout == null && moved.Changes.Any(c => c.Key.EndsWith("_ring")), "Ship movement updates bounded positions without replacing chart artwork or fog.");
        var fogOnlyRenderer = new NauticalChartRenderer();
        fogOnlyRenderer.Prepare(first, 640);
        var changedCells = first.Discovery.ToCharArray(); changedCells[0] = '0';
        var fogOnly = fogOnlyRenderer.Prepare(first with { Discovery = new(changedCells) }, 640);
        check(fogOnly.Layout == null && fogOnly.Changes is [{ Key: "chart_f0_0", Value: true }],
            "A single discovery-cell change updates only that fog bind, preserving the chart layout.");
        nav.SetDestination(ship, "ocean_01", 120, 90, 0);
        var single = Frame(ship, nav);
        check(single.Route is [{ X: 120, Y: 90, Final: true }] && NauticalChartProjection.Segments(single, 640).Length == 1 &&
            single.Motion == "Movement: UNDERWAY" && single.Mode == "Mode: ROUTE" && single.Waypoints == "Waypoints remaining: 1",
            "Single-destination navigation plots its actual endpoint and accurate movement/mode instruments.");
        var layout = renderer.Prepare(single, 640).Layout!;
        int routeIndex = layout.DrawList!.FindIndex(d => d is NuiDrawListLine { PointA: NuiBind<NuiVector> a } && a.Key.StartsWith("chart_r"));
        int fogIndex = layout.DrawList.FindIndex(d => d is NuiDrawListPolyLine { Enabled: NuiBind<bool> b } && b.Key.StartsWith("chart_f"));
        int ownIndex = layout.DrawList.FindIndex(d => d is NuiDrawListImage { ResRef: NuiBind<string> b } && b.Key.StartsWith("chart_m"));
        check(routeIndex > 0 && routeIndex < fogIndex && fogIndex < ownIndex, "Background, course, opaque fog and eligible own-ship sprite have explicit safe layer order.");
        check(layout.DrawList.Last() is NuiDrawListCircle { Enabled: NuiBind<bool> selected } && selected.Key == "chart_m0_selected" &&
            layout.DrawList.Take(ownIndex).OfType<NuiDrawListText>().Any(t => Value(t.Text) == "N"),
            "The actual own-ship drawing follows compass decoration and remains the foreground marker.");
        nav.SetDestination(ship, "ocean_01", 125, 95, 0);
        var shifted = renderer.Prepare(Frame(ship, nav), 640);
        check(shifted.Layout == null && shifted.Changes.Any(c => c.Key == "chart_r0_b") &&
            shifted.Changes.Any(c => c.Key == "chart_w0_rect"),
            "Changing a destination with the same route topology updates endpoint binds without a chart rebuild.");
        var unseen = Frame(ship, nav, new ChartDiscoveryService());
        var unseenUpdate = new NauticalChartRenderer().Prepare(unseen, 640);
        check(unseen.Route.Length == 1 && unseenUpdate.Changes.Any(c => c.Key == "chart_w0_on" && Equals(c.Value, false)) &&
            unseenUpdate.Changes.Any(c => c.Key == "chart_w0_final" && Equals(c.Value, false)),
            "Planning through fog does not expose destination marker or terrain; stored navigation remains visible in the readout.");
        var route = new ShipNavigationRoute { ShipName = ship.ShipName, Waypoints =
            [new() { AreaResRef = "ocean_01", X = 110, Y = 100, Description = "First" },
             new() { AreaResRef = "ocean_01", X = 120, Y = 100, Description = "Second" },
             new() { AreaResRef = "ocean_01", X = 130, Y = 100, Description = "Final" }] };
        nav.SetRoute(ship, route);
        var multiple = Frame(ship, nav);
        check(multiple.Route.Length == 3 && NauticalChartProjection.Segments(multiple, 640).Length == 3 &&
            multiple.Route.Count(p => p.Final) == 1 && multiple.Destination.Contains("130"), "All actual remaining local waypoints connect to a single final destination marker.");
        renderer.Prepare(multiple, 640); nav.AdvanceWaypoint(ship);
        var advanced = Frame(ship, nav);
        check(advanced.Route.Length == 2 && advanced.Route[0].X == 120 && advanced.Waypoints.EndsWith("2") &&
            renderer.Prepare(advanced, 640).Layout != null, "Completing a waypoint updates the actual remaining route and drawing topology.");
        nav.AdvanceWaypoint(ship); nav.AdvanceWaypoint(ship);
        check(Frame(ship, nav).Route.Length == 0 && Frame(ship, nav).Waypoints.EndsWith("0"), "A completed route leaves no stale course or final marker.");
        nav.SetRoute(ship, new() { ShipName = ship.ShipName, Waypoints = [new() { AreaResRef = "ocean_01", X = 150, Y = 100, Description = "Replacement" }] });
        check(Frame(ship, nav).Route.Single().X == 150, "Replacing navigation uses the new actual route without retaining a separate visual route.");
        nav.CancelNavigation(ship);
        check(Frame(ship, nav).Route.Length == 0 && Frame(ship, nav).Motion.EndsWith("STOPPED") && Frame(ship, nav).Mode.EndsWith("STOPPED"),
            "The existing STOP operation clears course visualization and movement readouts.");
        var manualShip = Ship("manual"); manualShip.Underway = true;
        var manualFrame = Frame(manualShip, nav);
        check(manualFrame.Motion == "Movement: UNDERWAY" && manualFrame.Mode == "Mode: MANUAL" && manualFrame.Route.Length == 0,
            "Manual travel reports the actual movement flag without inventing a numerical speed or route.");
        manualShip.ApplyHullDamage(manualShip.Hull);
        check(Frame(manualShip, nav).Motion == "Movement: DISABLED" && Frame(manualShip, nav).Route.Length == 0,
            "Disabled hull state has an accurate movement readout and cannot produce an active visual route.");
        var planned = new ShipRoutePlannerService(new SailingAreaService()).BuildRoute(ship.ShipName, "ocean_01", 90, 100, 0, "ocean_002", 100, 100, 0)!;
        nav.SetRoute(ship, planned);
        var cross = Frame(ship, nav);
        check(cross.Route.Length == 1 && cross.Route[0] is { Area: "ocean_01", X: 635, Final: false } &&
            cross.Destination.Contains("ocean_002") && NauticalChartProjection.Segments(cross, 640).All(s => s.To.X < 640),
            "A real cross-area planner route displays only the current ocean's boundary leg and the actual destination readout.");
        ship.AreaResRef = "ocean_002"; ship.X = 0; ship.Y = 80; nav.AdvanceWaypoint(ship);
        check(Frame(ship, nav).Route.Length == 2 && Frame(ship, nav).Route.All(p => p.Area == "ocean_002"),
            "After the engine changes area and navigation advances, only the new chart's entry/final legs are plotted.");
        string before = Persisted(ship); var savedWaypoints = planned.Waypoints.ToArray(); int index = planned.CurrentWaypointIndex;
        var discovery = new ChartDiscoveryService(); discovery.RevealAroundShip("a", ship); string beforeCells = discovery.CaptureCells("a", ship.AreaResRef);
        var encounters = new ShipEncounterService(); var other = Ship("other", 200, 100); other.AreaResRef = "ocean_002";
        encounters.CheckEncounters([ship, other]); other.X = 10; other.Y = 80;
        for (int n = 0; n < 10; n++) renderer.Prepare(Frame(ship, nav, discovery, fleet: [ship, other]), 640);
        check(before == Persisted(ship) && planned.CurrentWaypointIndex == index && planned.Waypoints.SequenceEqual(savedWaypoints) &&
            ReferenceEquals(nav.GetRoute(ship), planned), "Rendering never mutates navigation, route identity, ship movement or persisted ship state.");
        check(discovery.CaptureCells("a", ship.AreaResRef) == beforeCells, "Rendering never reveals cells or changes persisted chart data.");
        check(encounters.GetActiveEncounters().Count == 0, "Chart refresh cannot add an encounter for newly adjacent ships; global reconciliation remains owned by the existing tick.");
    }
    static void LayoutAndLifetime(Action<bool, string> check)
    {
        var ship = Ship("own"); var nav = Navigation(); var discovery = new ChartDiscoveryService();
        discovery.RevealAroundShip("a", ship);
        var remoteExploration = Ship("explorer", 600, 600); discovery.RevealAroundShip("b", remoteExploration);
        var a = Frame(ship, nav, discovery, "a"); var b = Frame(ship, nav, discovery, "b");
        var windows = new NauticalChartWindowCache(); var ca = windows.Open("viewer-a"); var cb = windows.Open("viewer-b");
        ca.Prepare(a, 640); cb.Prepare(b, 640);
        check(windows.Count == 2 && !ReferenceEquals(ca, cb) && a.Discovery != b.Discovery &&
            discovery.IsDiscovered("a", "ocean_01", 8, 7) && !discovery.IsDiscovered("b", "ocean_01", 8, 7),
            "Two players viewing the same ship have independent caches and the existing independent account discovery.");
        var npc = Ship("npc", 10, 10, ShipType.Merchant);
        Frame(ship, nav, discovery, "a", [ship, npc]); npc.X = 20;
        check(Frame(ship, nav, discovery, "a", [ship, npc]).Discovery == a.Discovery, "Unrelated NPC movement cannot reveal or alter the viewer's fog.");
        windows.Close("viewer-a");
        check(!windows.TryGet("viewer-a", out _) && windows.TryGet("viewer-b", out var remaining) && ReferenceEquals(remaining, cb),
            "Closing one chart removes its own cache and preserves another player's view.");
        var reopened = windows.Open("viewer-a");
        check(!ReferenceEquals(reopened, ca) && reopened.Prepare(a, 640).Layout != null && reopened.LayoutBuilds == 1,
            "Reopening the same chart creates fresh layout/bind publication rather than retaining stale token state.");
        check(reopened.Prepare(a, 608).Layout is { Width: 608, Height: 608 }, "Resizing rebuilds square pixel geometry and all fog/marker coordinates consistently.");
        windows.Clear(); check(windows.Count == 0, "Shutdown clears every chart cache.");
        var rendered = new NauticalChartRenderer().Prepare(Frame(ship, nav), 640).Layout!;
        check(rendered.Width == rendered.Height && Value(((NuiDrawListImage)rendered.DrawList![0]).ResRef) == "sailing_map" &&
            Value(((NuiDrawListImage)rendered.DrawList[0]).Rect) is { X: 0, Y: 0, Width: 640, Height: 640 },
            "The actual draw list preserves full square nautical artwork without stretching or inset offsets.");
        var fog = rendered.DrawList!.OfType<NuiDrawListPolyLine>().Where(d => d.Enabled is NuiBind<bool> bind && bind.Key.StartsWith("chart_f")).ToArray();
        check(fog.Length == 256 && fog.All(d => Value(d.Fill!) && Value(d.Color!).AlphaF == 1),
            "All 256 actual fog primitives are fully opaque fallback masks, preventing texture gaps or hidden terrain leakage.");
        Dictionary<Type, object> services = new() { [typeof(EventService)] = new EventService([]),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions { Enabled = false, Backend = SailingPersistenceBackend.Disabled },
            [typeof(SailingPersistenceContextFactory)] = SailingPersistenceContextFactory.ForOptions(
                new DbContextOptionsBuilder<SailingPersistenceDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options) };
        try
        {
            var service = (SailingNuiService)StartupChecks.CreateResolver(services)(typeof(SailingNuiService));
            var root = (NuiColumn)typeof(SailingNuiService).GetMethod("CreateLayout", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(service, [ship, 608f])!;
            var body = (NuiRow)root.Children[1]; var chart = (NuiGroup)((NuiColumn)body.Children[0]).Children[0]; var panel = (NuiGroup)body.Children[2];
            check(chart is { Width: 608, Height: 608, Margin: 0, Padding: 0 } && Value(chart.Scissor!) &&
                chart.Scrollbars == NuiScrollbars.None && panel is { Width: 320, Height: 664, Scrollbars: NuiScrollbars.None },
                "Actual layout reserves a scissored square chart and fixed instruments, with vertical scrolling confined to additional controls.");
            var side = (NuiColumn)panel.Element!;
            var buttons = NauticalChartRecoveryChecks.Elements(side).OfType<NuiButton>().Where(b => b.Id != "sailing_target_select").Select(b => b.Id).ToArray();
            var expected = new[] { "left_button", "ahead_button", "right_button", "astern_button", "stop_button", "dock_button", "hail_button",
                "trade_button", "port_market_button", "attack_button", "hail_contact_button", "board_button", "accept_board_button", "reject_board_button",
                "weapon_cannon_button", "weapon_ballista_button", "weapon_catapult_button", "weapon_heavy_cannon_button", "repair_button",
                "test_navigation_button", "leave_button" };
            check(buttons.Order().SequenceEqual(expected.Order()) && side.Children.OfType<NuiLabel>().All(l => l.Height >= 28),
                "Every existing Sailing command remains present, with readable labels and unchanged event identities.");
            var list = side.Children.OfType<NuiList>().Single();
            check(list.RowTemplate[1].Element.Id == "sailing_target_select" && ((NuiBind<int>)list.RowCount).Key == "sailing_target_count",
                "Sailing target selection remains its own row binding and does not use the Combat window's selection.");
        }
        finally { foreach (var service in services.Values.OfType<IDisposable>().Reverse()) service.Dispose(); }
    }
}
#endif
