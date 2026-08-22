namespace AmiaReforged.Core.Models.Sailing;

public sealed class ChartMarker
{
    public required string AreaResRef { get; init; }

    public required string Sprite { get; init; }

    public required float X { get; init; }

    public required float Y { get; init; }

    public float Size { get; init; } = 18f;
}