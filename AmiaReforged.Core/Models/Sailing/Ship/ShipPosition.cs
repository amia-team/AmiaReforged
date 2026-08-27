namespace AmiaReforged.Core.Models.Sailing.Ship;

public sealed record ShipPosition(string AreaResRef, float X, float Y)
{
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
