namespace AmiaReforged.Core.Models.Sailing;

public class ShipEncounter
{
    /// <summary>
    /// The first ship involved in the encounter.
    /// </summary>
    public required ShipState ShipA { get; set; }

    /// <summary>
    /// The second ship involved in the encounter.
    /// </summary>
    public required ShipState ShipB { get; set; }

    /// <summary>
    /// The sailing area where the encounter is occurring.
    /// </summary>
    public required string AreaResRef { get; set; }

    /// <summary>
    /// The current distance between the two ships.
    /// </summary>
    public float Distance { get; set; }
}