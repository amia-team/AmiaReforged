using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public sealed record ShipDefinition(
    string ShipName,
    string SpritePrefix,
    string HelmTag,
    string PlaceableTag,
    string ExitTag,
    string DeckAreaResRef,
    string CabinAreaResRef,
    string OceanAreaResRef,
    float X,
    float Y,
    float Z,
    Heading Heading,
    ShipType ShipType,
    int Hull,
    string WeaponResRef = "ship_cannon")
{
    public string StableId { get; init; } = ShipName; // Legacy persisted IDs remain unchanged when a display name is changed.
    public string? DefinitionId { get; init; }
    public bool AllowsWeapons { get; init; } = true;

    public int CargoCapacity { get; set; } = 50;

    /// <summary>Default storage applied only after persistence confirms this ship has no saved record.</summary>
    public string? InitialStoredPortId { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(StableId) || string.IsNullOrWhiteSpace(ShipName) || string.IsNullOrWhiteSpace(SpritePrefix) ||
            string.IsNullOrWhiteSpace(OceanAreaResRef) || !float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Z) ||
            !Enum.IsDefined(Heading) || !Enum.IsDefined(ShipType) || Hull <= 0 || CargoCapacity < 0 ||
            (AllowsWeapons ? string.IsNullOrWhiteSpace(WeaponResRef) : !string.IsNullOrEmpty(WeaponResRef)) ||
            (InitialStoredPortId != null && string.IsNullOrWhiteSpace(InitialStoredPortId)))
            throw new ArgumentException("Ship definition has invalid identity, position, hull, or equipment.");
        if (ShipType == ShipType.Player && (string.IsNullOrWhiteSpace(HelmTag) || string.IsNullOrWhiteSpace(PlaceableTag) ||
            string.IsNullOrWhiteSpace(ExitTag) || string.IsNullOrWhiteSpace(DeckAreaResRef) ||
            (DefinitionId != CutterRentalFleet.DefinitionId && string.IsNullOrWhiteSpace(CabinAreaResRef))))
            throw new ArgumentException("Player ships require helm, physical ship, exit and interior mappings.");
        if (DefinitionId == CutterRentalFleet.DefinitionId && (AllowsWeapons || ShipType != ShipType.Player))
            throw new ArgumentException("Rental cutters must be unarmed player ships.");
    }
}
