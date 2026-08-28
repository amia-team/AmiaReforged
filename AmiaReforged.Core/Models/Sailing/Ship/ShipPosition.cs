namespace AmiaReforged.Core.Models.Sailing.Ship;

/// <summary>
/// Represents the spatial position of a ship within the game world.
/// </summary>
/// <param name="AreaResRef">The ResRef of the area where the ship is located.</param>
/// <param name="X">The X-coordinate in the sailing area.</param>
/// <param name="Y">The Y-coordinate in the sailing area.</param>
public sealed record ShipPosition(string AreaResRef, float X, float Y)
{
    /// <summary>
    /// Calculates the Euclidean distance between this position and another.
    /// Returns <see cref="float.PositiveInfinity"/> if the positions are in different areas.
    /// </summary>
    /// <param name="other">The other position to measure distance to.</param>
    /// <returns>The distance between the two points, or infinity if in different areas.</returns>
    public float DistanceTo(ShipPosition other)
    {
        // Ships in different sailing areas are never in range
        if (AreaResRef != other.AreaResRef)
            return float.PositiveInfinity;

        float dx = other.X - X;
        float dy = other.Y - Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
