namespace AmiaReforged.Core.Models.Sailing;

public class ShipState
{
    /// <summary>
    /// The display name of the ship.
    /// </summary>
    public required string ShipName { get; set; }

    public required string DeckAreaResRef { get; set; }

    /// <summary>
    /// The PC key of the character currently at the helm.
    /// </summary>
    public string? HelmsmanPCKey { get; set; }

    /// <summary>
    /// The NWN area ResRef representing the ship's
    /// current location on the sailing map.
    /// </summary>
    public required string AreaResRef { get; set; }

    public ShipType ShipType { get; set; } = ShipType.Player;

    public string SpritePrefix { get; set; } = "sloop";
    public bool CanDock { get; set; }

    public string? NearbyIslandId { get; set; }
    
    /// <summary>
    /// The ship's X position on the abstract sailing map.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The ship's Y position on the abstract sailing map.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The ship's Z position on the abstract sailing map.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// The direction the ship is currently facing.
    /// </summary>
    public Heading Heading { get; set; } = Heading.East;

    /// <summary>
    /// Indicates whether the ship is currently underway.
    /// </summary>
    public bool Underway { get; set; }

    /// <summary>
    /// The NWN area ResRef where the ship is navigating to.
    /// Null when no destination has been assigned.
    /// </summary>
    public string? DestinationAreaResRef { get; set; }

    /// <summary>
    /// The destination X coordinate on the abstract sailing map.
    /// </summary>
    public float DestinationX { get; set; }

    /// <summary>
    /// The destination Y coordinate on the abstract sailing map.
    /// </summary>
    public float DestinationY { get; set; }

    /// <summary>
    /// The destination Z coordinate on the abstract sailing map.
    /// </summary>
    public float DestinationZ { get; set; }

    /// <summary>
    /// Indicates whether the ship currently has an active
    /// navigation course.
    /// </summary>
    public bool IsNavigating { get; set; }

    /// <summary>
    /// The ship's current hull integrity.
    /// </summary>
    public int Hull { get; set; } = 100;

  

    /// <summary>
    /// The ResRef of the weapon currently equipped
    /// on the ship.
    /// </summary>
    public string WeaponResRef { get; set; } =
        "ship_cannon";

    /// <summary>
/// True while the ship is performing a dock/undock transition.
/// Prevents duplicate NUI and navigation updates during area changes.
/// </summary>
public bool IsDocking { get; set; }
}