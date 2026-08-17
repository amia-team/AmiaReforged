namespace AmiaReforged.Core.Models.Sailing;

public class SailingLocation
{
    /// <summary>
    /// The NWN area ResRef where the player physically exists.
    /// </summary>
    public required string AreaResRef { get; set; }

    /// <summary>
    /// The physical NWN X coordinate.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The physical NWN Y coordinate.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The physical NWN Z coordinate.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// The physical NWN facing.
    /// </summary>
    public float Rotation { get; set; }
}