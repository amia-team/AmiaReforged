#if SAILING_REGRESSION
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services.Sailing;

static class CutterActivationChecks
{
    private static string FixtureDirectory([System.Runtime.CompilerServices.CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "Toolset"));
    public static void Run(Action<bool, string> check)
    {
        string? original = Environment.GetEnvironmentVariable("AMIA_SAILING_MARITIME_CONFIG");
        string? enabled = Environment.GetEnvironmentVariable("AMIA_SAILING_RENTALS_ENABLED");
        MaritimeOptions defaults, development;
        try
        {
            Environment.SetEnvironmentVariable("AMIA_SAILING_MARITIME_CONFIG", null);
            Environment.SetEnvironmentVariable("AMIA_SAILING_RENTALS_ENABLED", null);
            defaults = new();
            string docs = FixtureDirectory();
            Environment.SetEnvironmentVariable("AMIA_SAILING_MARITIME_CONFIG", Path.Combine(docs, "MARITIME_CONFIGURATION.example.json"));
            var example = new MaritimeOptions();
            check(!example.RentalsEnabled && example.RentalVessels.All(v => v.IsConfigured),
                "Example JSON maps both cutters but preserves deployment opt-in.");
            Environment.SetEnvironmentVariable("AMIA_SAILING_MARITIME_CONFIG", Path.Combine(docs, "MARITIME_CONFIGURATION.development.json"));
            development = new();
            check(development.RentalsEnabled && development.RentalVessels.Count == 2,
                "The actual loader enables two slots from the provided development configuration.");
            check(!defaults.RentalsEnabled && defaults.Markets.Select(m => m.PortId).SequenceEqual(development.Markets.Select(m => m.PortId)),
                "Development activation does not enable production defaults or replace maritime ports.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("AMIA_SAILING_MARITIME_CONFIG", original);
            Environment.SetEnvironmentVariable("AMIA_SAILING_RENTALS_ENABLED", enabled);
        }
        var port = new IslandService().GetIslands().Single(i => i.Id == "southport");
        check(port.ShipyardTag == "southport_shipyard" && port.LandingArea == "cordor_west" && port.OceanArea == "ocean_01",
            "Activation preserves the configured Southport shipyard and ocean.");
        var valid = new CutterResourceSnapshot { DeckCount = 1, RepresentationCount = 1, RepresentationInShipArea = true,
            HelmCount = 1, HelmInInterior = true, ExitCount = 1, DeckExitCount = 1, DeckSpawnCount = 1,
            SpawnLocationValid = true, UniqueInteriors = true };
        var rows = new List<DockyardRentalOffer>();
        for (int index = 0; index < 2; index++)
        {
            var slot = development.RentalVessels[index]; string prefix = $"sp_cutter_{index + 1:00}";
            var fallback = defaults.RentalVessels[index];
            check(slot.ShipId == $"southport-rental-cutter-{index + 1:00}" && slot.ShipName == $"Southport Cutter {index + 1:00}" &&
                slot.DeckAreaResRef == prefix && slot.HelmTag == prefix + "_helm" && slot.ExitTag == prefix + "_exit" &&
                slot.PlaceableTag == prefix + "_ship" && slot.CabinAreaResRef == "" && slot.PortId == "southport" &&
                slot.MaximumHull == 75 && slot.CargoCapacity == 60 && slot.Fee == 100 && slot.Deposit == 200,
                "Each development slot has precisely the supplied identity, resources and rental terms.");
            check(fallback.DeckAreaResRef == slot.DeckAreaResRef && fallback.HelmTag == slot.HelmTag &&
                fallback.ExitTag == slot.ExitTag && fallback.PlaceableTag == slot.PlaceableTag,
                "Fallback mappings and JSON mappings agree exactly.");
            var definition = CutterRentalFleet.CreateDefinition(slot, port);
            var ship = new ShipState { ShipId = definition.StableId, ShipName = definition.ShipName,
                DefinitionId = definition.DefinitionId!, DeckAreaResRef = definition.DeckAreaResRef,
                AreaResRef = definition.OceanAreaResRef, AllowsWeapons = definition.AllowsWeapons,
                CargoCapacity = definition.CargoCapacity, MaximumHull = definition.Hull, Hull = definition.Hull,
                RentalPoolId = "southport", WeaponResRef = definition.WeaponResRef };
            new ShipStorageService().StoreShip(ship, "southport");
            check(valid.Failures(slot, "ocean_01").Count == 0, "Complete observed resources pass the same validation used by live checkout.");
            var row = DockyardRentalOffer.Create(slot, ship, development.RentalsEnabled, valid.Failures(slot, ship.AreaResRef).Count == 0, false);
            rows.Add(row);
            check(row.CanRent && row.Name.Contains(slot.ShipName) && row.Terms.Contains("AVAILABLE FOR RENT") &&
                row.Terms.Contains("fee 100 gp + deposit 200 gp") && row.Specifications.Contains("Hull 75/75; cargo 60; UNARMED"),
                "The exact dockyard row projection displays each available cutter and all requested specifications.");
            foreach (var invalid in new[] { valid with { DeckCount = 0 }, valid with { DeckCount = 2 },
                valid with { HelmCount = 0 }, valid with { HelmCount = 2 }, valid with { HelmInInterior = false },
                valid with { ExitCount = 0, DeckExitCount = 0 }, valid with { ExitCount = 2 }, valid with { DeckExitCount = 0 },
                valid with { DeckSpawnCount = 0 }, valid with { DeckSpawnCount = 2 }, valid with { SpawnLocationValid = false },
                valid with { RepresentationCount = 0 }, valid with { RepresentationCount = 2 }, valid with { RepresentationInShipArea = false },
                valid with { UniqueInteriors = false } })
            {
                var failures = invalid.Failures(slot, ship.AreaResRef);
                var unavailable = DockyardRentalOffer.Create(slot, ship, true, failures.Count == 0, false);
                check(failures.Count > 0 && !unavailable.CanRent && unavailable.Name.Contains(slot.ShipName) &&
                    unavailable.Terms.Contains("REQUIRED MODULE RESOURCES"),
                    "A missing, duplicate or misplaced native resource produces a disabled named offer and diagnostic.");
            }
            check((valid with { DeckCount = 0 }).Failures(slot, ship.AreaResRef).Single().Contains(slot.DeckAreaResRef) &&
                (valid with { HelmCount = 0 }).Failures(slot, ship.AreaResRef).Single().Contains(slot.HelmTag) &&
                (valid with { ExitCount = 0 }).Failures(slot, ship.AreaResRef).Single().Contains(slot.ExitTag) &&
                (valid with { DeckSpawnCount = 0 }).Failures(slot, ship.AreaResRef).Single().Contains("SAILING_DECK_SPAWN"),
                "Startup diagnostics identify the exact deck, helm, exit and spawn requirement.");
            check(!DockyardRentalOffer.Create(slot, ship, true, true, true).CanRent,
                "An occupied cutter remains unavailable even with complete resources.");
            ship.IsInitializing = true;
            check(!DockyardRentalOffer.Create(slot, ship, true, true, false).CanRent, "Restoration gates new checkout.");
            ship.IsInitializing = false;
            check(DockyardRentalOffer.Create(slot, ship, true, true, false).CanRent, "Refreshing after restoration enables the eligible offer.");
            ship.Rental = new() { ShipId = ship.ShipId, Status = ShipRentalStatus.Active };
            check(!DockyardRentalOffer.Create(slot, ship, true, true, false).CanRent, "An active rental is never offered for double assignment.");
            ship.Rental = null;
            check(DockyardRentalOffer.Create(slot, ship, true, true, false).CanRent, "Refreshing after successful return shows reavailability.");
        }
        check(rows.Count == 2 && rows.All(r => r.CanRent) && rows.Select(r => r.ShipId).Distinct().Count() == 2 &&
            rows.All(r => r.ShipId != "Sea Sprite"), "Both offers populate independent rows; Sea Sprite is excluded from rentals.");
        var cabinSlot = new RentalVesselOption { DeckAreaResRef = "test_deck", CabinAreaResRef = "test_cabin" };
        check(valid.Failures(cabinSlot, "ocean_01").Count > 0 &&
            (valid with { CabinCount = 1, CabinExitCount = 1, ExitCount = 2 }).Failures(cabinSlot, "ocean_01").Count == 0,
            "An optional cabin still needs its own loaded area and exit; default deck-only cutters need neither.");
    }
}
#endif
