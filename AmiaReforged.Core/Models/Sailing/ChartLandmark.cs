namespace AmiaReforged.Core.Models.Sailing;

public sealed class ChartLandmark
{
    public required string AreaResRef { get; init; }

    public required string Sprite { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }
}