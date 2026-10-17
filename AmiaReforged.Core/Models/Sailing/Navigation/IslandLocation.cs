namespace AmiaReforged.Core.Models.Sailing;

public sealed class IslandLocation
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string OceanArea { get; set; } = string.Empty;

    public float OceanX { get; set; }

    public float OceanY { get; set; }

    public float DockRadius { get; set; }

    public string LandingArea { get; set; } = string.Empty;

    public float LandingX { get; set; }

    public float LandingY { get; set; }

    public float LandingZ { get; set; }

    public string? ShipyardTag { get; set; }
}