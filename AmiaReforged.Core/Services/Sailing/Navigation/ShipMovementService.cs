using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Atomic abstract movement; physical synchronization belongs to PhysicalShipService.</summary>
[ServiceBinding(typeof(ShipMovementService))]
public sealed class ShipMovementService(SailingAreaService areas, ShipObstacleService obstacles)
{
    public bool TryMove(ShipState ship, float distance)
    {
        if (!ship.IsActiveWorld || ship.IsDisabled || !float.IsFinite(distance) ||
            !areas.TryGetArea(ship.AreaResRef, out SailingArea? area) || area == null) return false;
        // Preserve the existing per-axis policy: diagonal distance is sqrt(2) times cardinal distance.
        var delta = ship.Heading switch
        {
            Heading.North => (0f, distance), Heading.NorthEast => (distance, distance),
            Heading.East => (distance, 0f), Heading.SouthEast => (distance, -distance),
            Heading.South => (0f, -distance), Heading.SouthWest => (-distance, -distance),
            Heading.West => (-distance, 0f), Heading.NorthWest => (-distance, distance), _ => (0f, 0f)
        };
        float x = ship.X + delta.Item1, y = ship.Y + delta.Item2, z = ship.Z;
        string nextArea = ship.AreaResRef;
        string? direction = x > area.MaxX ? "East" : x < area.MinX ? "West" :
            y > area.MaxY ? "North" : y < area.MinY ? "South" : null;
        if (direction != null)
        {
            string? destination = direction switch
            {
                "East" => area.EastAreaResRef, "West" => area.WestAreaResRef,
                "North" => area.NorthAreaResRef, "South" => area.SouthAreaResRef, _ => null
            };
            if (destination == null || !areas.TryGetArea(destination, out SailingArea? target) || target == null)
                return false;
            string entrySide = direction switch { "East" => "West", "West" => "East", "North" => "South", _ => "North" };
            SailingLocation? entry = areas.GetEntryLocation(destination, entrySide);
            x = entry?.X ?? (direction == "East" ? target.MinX + 5 : direction == "West" ? target.MaxX - 5 :
                Math.Clamp(ship.X, target.MinX, target.MaxX));
            y = entry?.Y ?? (direction == "North" ? target.MinY + 5 : direction == "South" ? target.MaxY - 5 :
                Math.Clamp(ship.Y, target.MinY, target.MaxY));
            z = entry?.Z ?? ship.Z;
            if (x < target.MinX || x > target.MaxX || y < target.MinY || y > target.MaxY) return false;
            nextArea = destination;
        }
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) || !obstacles.CanMoveTo(nextArea, x, y)) return false;
        ship.AreaResRef = nextArea;
        ship.X = x;
        ship.Y = y;
        ship.Z = z;
        return true;
    }
}
