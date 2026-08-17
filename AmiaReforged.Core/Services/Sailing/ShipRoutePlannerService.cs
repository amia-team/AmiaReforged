using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipRoutePlannerService))]
public class ShipRoutePlannerService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly SailingAreaService
        _sailingAreaService;

    public ShipRoutePlannerService(
        SailingAreaService sailingAreaService)
    {
        _sailingAreaService =
            sailingAreaService;

        Log.Info(
            "Ship Route Planner Service initialized.");
    }

    // -----------------------------------------------------------------
    // Build Route
    // -----------------------------------------------------------------

    public ShipNavigationRoute?
        BuildRoute(
            string shipName,
            string startAreaResRef,
            float startX,
            float startY,
            float startZ,
            string destinationAreaResRef,
            float destinationX,
            float destinationY,
            float destinationZ)
    {
        if (!_sailingAreaService.ContainsArea(
                startAreaResRef))
        {
            Log.Warn(
                $"Cannot build route: " +
                $"unknown starting area " +
                $"'{startAreaResRef}'.");

            return null;
        }

        if (!_sailingAreaService.ContainsArea(
                destinationAreaResRef))
        {
            Log.Warn(
                $"Cannot build route: " +
                $"unknown destination area " +
                $"'{destinationAreaResRef}'.");

            return null;
        }

        List<string>? areaRoute =
            FindAreaRoute(
                startAreaResRef,
                destinationAreaResRef);

        if (areaRoute == null)
        {
            Log.Warn(
                $"No sailing route found: " +
                $"Ship={shipName}, " +
                $"Start={startAreaResRef}, " +
                $"Destination={destinationAreaResRef}");

            return null;
        }

        ShipNavigationRoute route =
            new()
            {
                ShipName =
                    shipName
            };

        // -------------------------------------------------------------
        // Same-area destination
        // -------------------------------------------------------------

        if (areaRoute.Count == 1)
        {
            route.Waypoints.Add(
                new ShipNavigationWaypoint
                {
                    AreaResRef =
                        destinationAreaResRef,

                    X =
                        destinationX,

                    Y =
                        destinationY,

                    Z =
                        destinationZ,

                    Description =
                        "Final destination"
                });

            return route;
        }

        // -------------------------------------------------------------
        // Multi-area route
        // -------------------------------------------------------------

        string currentArea =
            startAreaResRef;

        float currentX =
            startX;

        float currentY =
            startY;

        float currentZ =
            startZ;

        for (
            int i = 1;
            i < areaRoute.Count;
            i++)
        {
            string nextArea =
                areaRoute[i];

            string direction =
                _sailingAreaService.GetConnectionDirection(
                    currentArea,
                    nextArea);

            if (string.IsNullOrWhiteSpace(
                    direction))
            {
                Log.Warn(
                    $"Cannot build route: " +
                    $"no connection from " +
                    $"'{currentArea}' to " +
                    $"'{nextArea}'.");

                return null;
            }

            // ---------------------------------------------------------
            // Add boundary waypoint in current area.
            // ---------------------------------------------------------

            ShipNavigationWaypoint? boundaryWaypoint =
                CreateBoundaryWaypoint(
                    currentArea,
                    currentX,
                    currentY,
                    currentZ,
                    direction);

            if (boundaryWaypoint != null)
            {
                route.Waypoints.Add(
                    boundaryWaypoint);
            }

            // ---------------------------------------------------------
            // Move into next sailing area.
            // ---------------------------------------------------------

            currentArea =
                nextArea;

            // ---------------------------------------------------------
            // Find actual entry point into new area.
            // ---------------------------------------------------------

            SailingLocation? entry =
                _sailingAreaService.GetEntryLocation(
                    currentArea,
                    direction);

            if (entry != null)
            {
                currentX =
                    entry.X;

                currentY =
                    entry.Y;

                currentZ =
                    entry.Z;

                route.Waypoints.Add(
                    new ShipNavigationWaypoint
                    {
                        AreaResRef =
                            currentArea,

                        X =
                            currentX,

                        Y =
                            currentY,

                        Z =
                            currentZ,

                        Description =
                            $"{direction} area entry"
                    });
            }
            else
            {
                Log.Warn(
                    $"No entry location configured: " +
                    $"Area={currentArea}, " +
                    $"Direction={direction}.");
            }
        }

        // -------------------------------------------------------------
        // Final destination
        // -------------------------------------------------------------

        route.Waypoints.Add(
            new ShipNavigationWaypoint
            {
                AreaResRef =
                    destinationAreaResRef,

                X =
                    destinationX,

                Y =
                    destinationY,

                Z =
                    destinationZ,

                Description =
                    "Final destination"
            });

        return route;
    }

    // -----------------------------------------------------------------
    // Area Graph Search
    // -----------------------------------------------------------------

    private List<string>?
        FindAreaRoute(
            string startArea,
            string destinationArea)
    {
        if (string.Equals(
                startArea,
                destinationArea,
                StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                startArea
            };
        }

        Queue<string> queue =
            new();

        Dictionary<string, string?>
            previous =
                new(
                    StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(
            startArea);

        previous[startArea] =
            null;

        while (queue.Count > 0)
        {
            string current =
                queue.Dequeue();

            foreach (
                string neighbor
                in _sailingAreaService.GetNeighbors(
                    current))
            {
                if (previous.ContainsKey(
                        neighbor))
                {
                    continue;
                }

                previous[neighbor] =
                    current;

                if (string.Equals(
                        neighbor,
                        destinationArea,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BuildAreaPath(
                        previous,
                        destinationArea);
                }

                queue.Enqueue(
                    neighbor);
            }
        }

        return null;
    }

    private List<string>
        BuildAreaPath(
            Dictionary<string, string?>
                previous,
            string destination)
    {
        List<string> path =
            new();

        string? current =
            destination;

        while (current != null)
        {
            path.Add(
                current);

            current =
                previous[current];
        }

        path.Reverse();

        return path;
    }

    // -----------------------------------------------------------------
    // Boundary Waypoints
    // -----------------------------------------------------------------

    private ShipNavigationWaypoint?
        CreateBoundaryWaypoint(
            string areaResRef,
            float currentX,
            float currentY,
            float currentZ,
            string direction)
    {
        SailingArea? area =
            _sailingAreaService.GetArea(
                areaResRef);

        if (area == null)
        {
            return null;
        }

        const float boundaryOffset =
            5.0f;

        return direction switch
        {
            "North" =>
                new ShipNavigationWaypoint
                {
                    AreaResRef =
                        areaResRef,

                    X =
                        currentX,

                    Y =
                        area.MaxY -
                        boundaryOffset,

                    Z =
                        currentZ,

                    Description =
                        "North area boundary"
                },

            "South" =>
                new ShipNavigationWaypoint
                {
                    AreaResRef =
                        areaResRef,

                    X =
                        currentX,

                    Y =
                        area.MinY +
                        boundaryOffset,

                    Z =
                        currentZ,

                    Description =
                        "South area boundary"
                },

            "East" =>
                new ShipNavigationWaypoint
                {
                    AreaResRef =
                        areaResRef,

                    X =
                        area.MaxX -
                        boundaryOffset,

                    Y =
                        currentY,

                    Z =
                        currentZ,

                    Description =
                        "East area boundary"
                },

            "West" =>
                new ShipNavigationWaypoint
                {
                    AreaResRef =
                        areaResRef,

                    X =
                        area.MinX +
                        boundaryOffset,

                    Y =
                        currentY,

                    Z =
                        currentZ,

                    Description =
                        "West area boundary"
                },

            _ =>
                null
        };
    }
}