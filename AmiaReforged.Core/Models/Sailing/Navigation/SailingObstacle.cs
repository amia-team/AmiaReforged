namespace AmiaReforged.Core.Models.Sailing;

public class SailingObstacle
{
    /// <summary>
    /// Display name of the obstacle.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Sailing area containing the obstacle.
    /// </summary>
    public required string AreaResRef { get; set; }

    /// <summary>
    /// Minimum X coordinate of the obstacle.
    /// </summary>
    public float MinX { get; set; }

    /// <summary>
    /// Maximum X coordinate of the obstacle.
    /// </summary>
    public float MaxX { get; set; }

    /// <summary>
    /// Minimum Y coordinate of the obstacle.
    /// </summary>
    public float MinY { get; set; }

    /// <summary>
    /// Maximum Y coordinate of the obstacle.
    /// </summary>
    public float MaxY { get; set; }
}