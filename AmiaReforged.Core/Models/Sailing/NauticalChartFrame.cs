namespace AmiaReforged.Core.Models.Sailing;

public readonly record struct ChartPoint(float X, float Y);
public readonly record struct ChartRect(float X, float Y, float Width, float Height);
public enum ChartContactKind { Neutral, Player, Merchant, Pirate, OwnShip }
public sealed record ChartShipMarker(string Id, string Name, float X, float Y, Heading? Heading,
    string Sprite, ChartContactKind Kind, bool Selected = false);
public sealed record ChartFeature(string Id, string Name, float X, float Y, string Sprite, float Width, float Height, bool Port);
public sealed record ChartRoutePoint(string Area, float X, float Y, bool Final);
public sealed record ChartRouteSegment(ChartPoint From, ChartPoint To);

/// <summary>Read-only presentation snapshot, never an alternate navigation route or discovery store.</summary>
public sealed record NauticalChartFrame(string ShipId, string Area, string Discovery,
    ChartShipMarker[] Markers, ChartFeature[] Features, ChartRoutePoint[] Route,
    string Heading, string Motion, string Mode, string Destination, string Waypoints);
