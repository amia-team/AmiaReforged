using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Only configured ship contacts become ships; wrecks and hazards remain contacts.</summary>
public static class OceanContactShipFactory
{
    public static ShipState? Create(OceanContact contact)
    {
        if (contact.Type is not (EncounterType.Pirate or EncounterType.Merchant) ||
            string.IsNullOrWhiteSpace(contact.Id) || string.IsNullOrWhiteSpace(contact.Name) ||
            string.IsNullOrWhiteSpace(contact.AreaResRef) || !float.IsFinite(contact.X) ||
            !float.IsFinite(contact.Y) || !float.IsFinite(contact.Z)) return null;
        return new ShipState
        {
            ShipId = contact.Id, DefinitionId = contact.Id, ShipName = contact.Name,
            RepresentationTag = contact.ShipTag, RepresentationResRef = contact.ShipResRef,
            ShipType = contact.Type == EncounterType.Pirate ? ShipType.Pirate : ShipType.Merchant,
            DeckAreaResRef = contact.Type == EncounterType.Pirate ? "pirate_brig_d" : "merchant_ship_d",
            AreaResRef = contact.AreaResRef, X = contact.X, Y = contact.Y, Z = contact.Z,
            Heading = Heading.West, MaximumHull = 100, Hull = 100, WeaponResRef = "ship_cannon",
            // Pirate AI owns pursuit; merchants require an explicitly configured route.
            Underway = false,
            SpritePrefix = contact.ShipResRef switch
            {
                "stormrunner" or "pirate_brig" => "brig", "black_pearl" => "galleon", "golden_gull" => "cog", _ => "sloop"
            }
        };
    }
}
