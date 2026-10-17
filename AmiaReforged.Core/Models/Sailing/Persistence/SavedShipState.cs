using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Sailing;

public class SavedShipState
{
    public int Id { get; set; }
    // The shared AmiaDbContext retains its existing schema. Sailing's dedicated
    // context explicitly maps these additions after an additive schema update.
    [NotMapped]
    public string? ShipId { get; set; }
    /// <summary>Versioned additional state; requires an additive database migration before enabling saves.</summary>
    [NotMapped]
    public string? SnapshotJson { get; set; }

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
