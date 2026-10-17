namespace AmiaReforged.Core.Models.Sailing;

public readonly record struct ChartPoint(float X, float Y);
public readonly record struct ChartRect(float X, float Y, float Width, float Height);
public enum ChartContactKind { Neutral, Player, Merchant, Pirate, OwnShip }
public enum SailingChartMode { Exploration, Tactical }
public sealed record ChartShipMarker(string Id, string Name, float X, float Y, Heading? Heading,
    string Sprite, ChartContactKind Kind, bool Selected = false);
public sealed record ChartFeature(string Id, string Name, float X, float Y, string Sprite, float Width, float Height, bool Port);
public sealed record ChartRoutePoint(string Area, float X, float Y, bool Final);
public sealed record ChartRouteSegment(ChartPoint From, ChartPoint To);

/// <summary>Read-only presentation snapshot, never an alternate navigation route or discovery store.</summary>
public sealed record NauticalChartFrame(string ShipId, string Area, string Discovery,
    ChartShipMarker[] Markers, ChartFeature[] Features, ChartRoutePoint[] Route,
    string Heading, string Motion, string Mode, string Destination, string Waypoints)
{
    public ChartRoutePoint[] Preview { get; init; } = [];
    public bool ChartCourse { get; init; }
    public SailingChartMode ChartMode { get; init; }
    public TacticalChartStatus? Tactical { get; init; }
    public ChartViewport? Viewport { get; init; }
}

/// <summary>Presentation only; target identity comes exclusively from the Sailing selection.</summary>
public sealed record TacticalChartStatus(ShipWeapon Weapon, string? TargetId, string TargetName,
    float? Distance, double? RelativeBearing, bool InRange, bool InArc, string AttackStatus)
{
    public bool WeaponsAvailable { get; init; }
}
