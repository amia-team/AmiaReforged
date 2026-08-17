namespace AmiaReforged.Core.Models.Sailing;

public class SavedShipState
{
    public int Id { get; set; }

    public required string ShipName { get; set; }

    public required string AreaResRef { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public Heading Heading { get; set; }

    public bool Underway { get; set; }

    public int Hull { get; set; }

    /// <summary>
    /// The ResRef of the weapon currently equipped
    /// on the ship.
    /// </summary>
    public string WeaponResRef { get; set; } =
        "ship_cannon";
}