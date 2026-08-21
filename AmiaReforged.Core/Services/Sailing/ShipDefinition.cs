namespace AmiaReforged.Core.Models.Sailing;

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
    string WeaponResRef = "ship_cannon");