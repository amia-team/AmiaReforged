using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>A bounded, persistent fleet. Each configured slot owns distinct preplaced interiors and tags for its lifetime.</summary>
public static class CutterRentalFleet
{
    public const string DefinitionId = "Cutter";
    public static List<RentalVesselOption> DefaultSlots() =>
    [
        new() { DeckAreaResRef = "sp_cutter_01", HelmTag = "sp_cutter_01_helm",
            ExitTag = "sp_cutter_01_exit", PlaceableTag = "sp_cutter_01_ship" },
        new() { ShipId = "southport-rental-cutter-02", ShipName = "Southport Cutter 02",
            DeckAreaResRef = "sp_cutter_02", HelmTag = "sp_cutter_02_helm",
            ExitTag = "sp_cutter_02_exit", PlaceableTag = "sp_cutter_02_ship" }
    ];

    public static bool IsCutter(ShipState ship) => ship.DefinitionId == DefinitionId;

    public static void ValidateIsolation(IEnumerable<RentalVesselOption> slots, IEnumerable<ShipDefinition>? otherShips = null)
    {
        var areas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var other in otherShips ?? [])
        {
            foreach (string area in new[] { other.DeckAreaResRef, other.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a))) areas.Add(area);
            foreach (string tag in new[] { other.HelmTag, other.ExitTag, other.PlaceableTag }.Where(t => !string.IsNullOrWhiteSpace(t))) tags.Add(tag);
            identities.Add(other.StableId); identities.Add(other.ShipName);
        }
        foreach (var slot in slots)
        {
            if (identities.Contains(slot.ShipId) || identities.Contains(slot.ShipName)) throw new ArgumentException("A rental cutter cannot replace an existing ship.");
            identities.Add(slot.ShipId); identities.Add(slot.ShipName);
            // Even partial mappings reserve their names: completing another slot cannot reuse them.
            foreach (string area in new[] { slot.DeckAreaResRef, slot.CabinAreaResRef }.Where(a => !string.IsNullOrWhiteSpace(a)))
                if (area.Length > 16 || !areas.Add(area)) throw new ArgumentException("Rental cutter deck/cabin ResRefs must be distinct and at most 16 characters.");
            foreach (string tag in new[] { slot.HelmTag, slot.ExitTag, slot.PlaceableTag }.Where(t => !string.IsNullOrWhiteSpace(t)))
                if (tag is "combat_station" or "southport_shipyard" || !tags.Add(tag)) throw new ArgumentException("Rental cutter interaction tags must be distinct from each other and existing ships.");
        }
    }

    public static ShipDefinition CreateDefinition(RentalVesselOption slot, IslandLocation port)
    {
        if (!slot.IsConfigured || port.Id != slot.PortId) throw new ArgumentException("Cutter requires a configured deck, helm, exit, representation and rental port.");
        ShipDefinition definition = new(slot.ShipName, slot.SpritePrefix, slot.HelmTag, slot.PlaceableTag, slot.ExitTag,
            slot.DeckAreaResRef, slot.CabinAreaResRef, port.OceanArea, port.OceanX, port.OceanY, 0,
            Heading.East, ShipType.Player, slot.MaximumHull, "")
        { StableId = slot.ShipId, DefinitionId = DefinitionId, AllowsWeapons = false, CargoCapacity = slot.CargoCapacity, InitialStoredPortId = port.Id };
        definition.Validate();
        return definition;
    }
}
