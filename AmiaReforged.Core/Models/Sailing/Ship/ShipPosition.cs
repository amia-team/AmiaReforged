namespace AmiaReforged.Core.Models.Sailing.Ship;

public sealed record ShipPosition(string AreaResRef, float X, float Y, float Z)
{
    /// <summary>
    /// The sailing area containing the ship.
    /// </summary>
    public string AreaResRef { get; } = AreaResRef;

    public float X { get; } = X;

    public float Y { get; } = Y;

    public float Z { get; } = Z;
}
