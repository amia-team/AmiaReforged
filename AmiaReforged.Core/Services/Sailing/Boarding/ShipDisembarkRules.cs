using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>A fixed port destination. Never derived from a physical ocean ship's location.</summary>
public sealed record ShipLandfall(string PortId, string PortName, string AreaResRef, float X, float Y, float Z);

public static class ShipDisembarkRules
{
    public const string UnavailableMessage = "You cannot disembark here. Approach a port or arrange boarding with another vessel.";

    public static bool InDockRange(ShipState ship, IslandLocation port) =>
        string.Equals(ship.AreaResRef, port.OceanArea, StringComparison.OrdinalIgnoreCase) &&
        float.IsFinite(ship.X) && float.IsFinite(ship.Y) && float.IsFinite(ship.Z) && float.IsFinite(port.OceanX) && float.IsFinite(port.OceanY) &&
        float.IsFinite(port.DockRadius) && port.DockRadius > 0 &&
        DistanceSquared(ship, port) <= (double)port.DockRadius * port.DockRadius;

    public static bool ValidLandfall(IslandLocation port, Func<string, bool> isOcean) =>
        !string.IsNullOrWhiteSpace(port.Id) && !string.IsNullOrWhiteSpace(port.LandingArea) &&
        !string.Equals(port.LandingArea, port.OceanArea, StringComparison.OrdinalIgnoreCase) && !isOcean(port.LandingArea) &&
        float.IsFinite(port.LandingX) && float.IsFinite(port.LandingY) && float.IsFinite(port.LandingZ) &&
        port.LandingX >= 0 && port.LandingY >= 0;

    public static ShipLandfall? Resolve(ShipState ship, IEnumerable<IslandLocation> ports, Func<string, bool> isOcean)
    {
        if (!ship.IsActiveWorld) return null;
        IslandLocation? port = ports.Where(p => InDockRange(ship, p) && ValidLandfall(p, isOcean) &&
                !string.Equals(p.LandingArea, ship.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(p.LandingArea, ship.CabinAreaResRef, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => DistanceSquared(ship, p)).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
        return port == null ? null : new(port.Id, port.Name, port.LandingArea, port.LandingX, port.LandingY, port.LandingZ);
    }

    public static bool StillEligible(ShipState ship, ShipLandfall destination, IEnumerable<IslandLocation> ports,
        Func<string, bool> isOcean) => ship.IsActiveWorld && ports.Any(p => p.Id == destination.PortId &&
            InDockRange(ship, p) && ValidLandfall(p, isOcean) && p.LandingArea == destination.AreaResRef &&
            p.LandingX == destination.X && p.LandingY == destination.Y && p.LandingZ == destination.Z &&
            !string.Equals(p.LandingArea, ship.DeckAreaResRef, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(p.LandingArea, ship.CabinAreaResRef, StringComparison.OrdinalIgnoreCase));

    public static bool DestinationAvailable(ShipLandfall destination, int matchingAreas, bool walkable,
        Func<string, bool> isOcean, bool isShipInterior = false) => matchingAreas == 1 && walkable && !isShipInterior && !string.IsNullOrWhiteSpace(destination.AreaResRef) &&
        !isOcean(destination.AreaResRef) && float.IsFinite(destination.X) && float.IsFinite(destination.Y) &&
        float.IsFinite(destination.Z) && destination.X >= 0 && destination.Y >= 0;

    private static double DistanceSquared(ShipState ship, IslandLocation port) =>
        Math.Pow((double)ship.X - port.OceanX, 2) + Math.Pow((double)ship.Y - port.OceanY, 2);
}
