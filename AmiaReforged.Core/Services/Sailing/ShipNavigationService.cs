using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipNavigationService))]
public class ShipNavigationService
{
    private const float DestinationThreshold =
        1.0f;

    private const float DefaultLookAheadDistance =
        1.0f;

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly ShipObstacleService
        _shipObstacleService;

    // -----------------------------------------------------------------
    // Temporary obstacle avoidance state
    // -----------------------------------------------------------------
    private readonly SailingAreaService _sailingAreaService;
    private readonly Dictionary<
    string,
    ShipNavigationRoute>
    _routes = new();

    private readonly Dictionary<
        string,
        Heading>
        _avoidanceHeadings =
            new();

 public ShipNavigationService(
    ShipObstacleService shipObstacleService,
    SailingAreaService sailingAreaService)
{
    _shipObstacleService = shipObstacleService;
    _sailingAreaService = sailingAreaService;
}

// -----------------------------------------------------------------
// Waypoint Route
// -----------------------------------------------------------------

public void SetRoute(
    ShipState ship,
    ShipNavigationRoute route)
{
    _routes[ship.ShipName] = route;

    ShipNavigationWaypoint? waypoint =
        route.CurrentWaypoint;

    if (waypoint != null)
    {
        SetDestination(
            ship,
            waypoint.AreaResRef,
            waypoint.X,
            waypoint.Y,
            waypoint.Z);
    }

    Log.Info(
        $"Navigation route set: " +
        $"Ship={ship.ShipName}, " +
        $"Waypoints={route.Waypoints.Count}");
}

public void ClearRoute(
    ShipState ship)
{
    if (_routes.Remove(
            ship.ShipName))
    {
        Log.Info(
            $"Navigation route cleared: " +
            $"Ship={ship.ShipName}");
    }
}

public ShipNavigationRoute?
GetRoute(
    ShipState ship)
{
    if (_routes.TryGetValue(
            ship.ShipName,
            out ShipNavigationRoute? route))
    {
        return route;
    }

    return null;
}

public ShipNavigationWaypoint?
GetCurrentWaypoint(
    ShipState ship)
{
    return GetRoute(ship)
        ?.CurrentWaypoint;
}
public bool AdvanceWaypoint(
    ShipState ship)
{
    ShipNavigationRoute? route =
        GetRoute(ship);

    if (route == null)
    {
        return false;
    }

    if (route.IsComplete)
    {
        return false;
    }

    ShipNavigationWaypoint? waypoint =
        route.CurrentWaypoint;

    Log.Info(
        $"Navigation waypoint reached: " +
        $"Ship={ship.ShipName}, " +
        $"Waypoint={route.CurrentWaypointIndex}, " +
        $"Area={waypoint?.AreaResRef}, " +
        $"X={waypoint?.X:0.00}, " +
        $"Y={waypoint?.Y:0.00}");

route.CurrentWaypointIndex++;

if (route.Loop &&
    route.CurrentWaypointIndex >= route.Waypoints.Count)
{
    route.CurrentWaypointIndex = 0;

    Log.Info(
        $"Navigation route looping: " +
        $"Ship={ship.ShipName}");
}

if (route.IsComplete)
{
    Log.Info(
        $"Navigation route complete: " +
        $"Ship={ship.ShipName}");

    return true;
}
    ShipNavigationWaypoint next =
        route.CurrentWaypoint!;

    Log.Info(
        $"Navigation advancing to waypoint: " +
        $"Ship={ship.ShipName}, " +
        $"Waypoint={route.CurrentWaypointIndex}, " +
        $"Area={next.AreaResRef}, " +
        $"X={next.X:0.00}, " +
        $"Y={next.Y:0.00}");

    return false;
}
    // -----------------------------------------------------------------
    // Destination
    // -----------------------------------------------------------------

    public bool SetDestination(
        ShipState ship,
        string destinationAreaResRef,
        float destinationX,
        float destinationY,
        float destinationZ)
    {
        if (string.IsNullOrWhiteSpace(
                destinationAreaResRef))
        {
            Log.Warn(
                $"Cannot set navigation destination for " +
                $"'{ship.ShipName}': destination area is empty.");

            return false;
        }

        // New destination means any previous
        // obstacle avoidance state is no longer relevant.

        ClearAvoidanceHeading(
            ship);

        ship.DestinationAreaResRef =
            destinationAreaResRef;

        ship.DestinationX =
            destinationX;

        ship.DestinationY =
            destinationY;

        ship.DestinationZ =
            destinationZ;

        ship.IsNavigating =
            true;

        Log.Info(
            $"Navigation destination set: " +
            $"Ship={ship.ShipName}, " +
            $"Area={ship.DestinationAreaResRef}, " +
            $"X={ship.DestinationX:0.00}, " +
            $"Y={ship.DestinationY:0.00}, " +
            $"Z={ship.DestinationZ:0.00}");

        return true;
    }

    public void ClearDestination(
        ShipState ship)
    {
        ClearAvoidanceHeading(
            ship);

        ship.DestinationAreaResRef =
            null;

        ship.DestinationX =
            0.0f;

        ship.DestinationY =
            0.0f;

        ship.DestinationZ =
            0.0f;

        ship.IsNavigating =
            false;

        Log.Info(
            $"Navigation destination cleared: " +
            $"Ship={ship.ShipName}");
    }

    public bool IsNavigating(
        ShipState ship)
    {
        return ship.IsNavigating &&
               !string.IsNullOrWhiteSpace(
                   ship.DestinationAreaResRef);
    }

    // -----------------------------------------------------------------
    // Desired Heading
    // -----------------------------------------------------------------

public Heading GetDesiredHeading(
    ShipState ship)
{
    ShipNavigationWaypoint? waypoint =
        GetCurrentWaypoint(ship);

    // -------------------------------------------------------------
    // If the current waypoint is in another area,
    // head toward the boundary that connects to it.
    // -------------------------------------------------------------

    if (waypoint != null &&
        !string.Equals(
            waypoint.AreaResRef,
            ship.AreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        if (_sailingAreaService.TryGetArea(
                ship.AreaResRef,
                out SailingArea? area))
        {
            if (string.Equals(
                    area.EastAreaResRef,
                    waypoint.AreaResRef,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Heading.East;
            }

            if (string.Equals(
                    area.WestAreaResRef,
                    waypoint.AreaResRef,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Heading.West;
            }

            if (string.Equals(
                    area.NorthAreaResRef,
                    waypoint.AreaResRef,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Heading.North;
            }

            if (string.Equals(
                    area.SouthAreaResRef,
                    waypoint.AreaResRef,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Heading.South;
            }
        }
    }

    // -------------------------------------------------------------
    // Normal same-area navigation.
    // -------------------------------------------------------------

    float targetX =
        waypoint?.X ??
        ship.DestinationX;

    float targetY =
        waypoint?.Y ??
        ship.DestinationY;

    float deltaX =
        targetX - ship.X;

    float deltaY =
        targetY - ship.Y;

    bool east =
        deltaX > DestinationThreshold;

    bool west =
        deltaX < -DestinationThreshold;

    bool north =
        deltaY > DestinationThreshold;

    bool south =
        deltaY < -DestinationThreshold;

    if (north && east) return Heading.NorthEast;
    if (north && west) return Heading.NorthWest;
    if (south && east) return Heading.SouthEast;
    if (south && west) return Heading.SouthWest;
    if (north) return Heading.North;
    if (south) return Heading.South;
    if (east) return Heading.East;
    if (west) return Heading.West;

    return ship.Heading;
}

public bool IsWaitingForAreaTransition(
    ShipState ship)
{
    ShipNavigationWaypoint? waypoint =
        GetCurrentWaypoint(
            ship);

    if (waypoint == null)
    {
        return false;
    }

    return !string.Equals(
        ship.AreaResRef,
        waypoint.AreaResRef,
        StringComparison.OrdinalIgnoreCase);
}
    // -----------------------------------------------------------------
    // Current Waypoint
    // -----------------------------------------------------------------

    public bool IsCurrentWaypointReached(
    ShipState ship)
{
    ShipNavigationWaypoint? waypoint =
        GetCurrentWaypoint(
            ship);

    if (waypoint == null)
    {
        return false;
        }
        

        // -------------------------------------------------------------
        // The waypoint must belong to the ship's current area.
        //
        // This prevents a boundary waypoint from advancing into the
        // next area's waypoint before CrossBoundary() occurs.
        // -------------------------------------------------------------

        if (!string.Equals(
            ship.AreaResRef,
            waypoint.AreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    float deltaX =
        waypoint.X -
        ship.X;

    float deltaY =
        waypoint.Y -
        ship.Y;

    float distance =
        MathF.Sqrt(
            deltaX * deltaX +
            deltaY * deltaY);

    return distance <=
           DestinationThreshold;
}
    //--------------------------------------------------
    // Obstacle-Aware Navigation
    // -----------------------------------------------------------------

    public Heading GetNavigationHeading(
        ShipState ship,
        float lookAheadDistance =
            DefaultLookAheadDistance)
    {
        Heading desiredHeading =
            GetDesiredHeading(
                ship);

        // -------------------------------------------------------------
        // Direct route is clear.
        //
        // Resume normal navigation and forget any
        // previous obstacle avoidance heading.
        // -------------------------------------------------------------

        if (!IsHeadingBlocked(
                ship,
                desiredHeading,
                lookAheadDistance))
        {
            if (TryGetAvoidanceHeading(
                    ship,
                    out Heading previousAvoidance))
            {
                Log.Info(
                    $"Navigation obstacle cleared: " +
                    $"Ship={ship.ShipName}, " +
                    $"Avoidance={previousAvoidance}, " +
                    $"Resuming={desiredHeading}");

                ClearAvoidanceHeading(
                    ship);
            }

            return desiredHeading;
        }

        // -------------------------------------------------------------
        // Direct route is blocked.
        // -------------------------------------------------------------

        Log.Info(
            $"Navigation obstacle detected for " +
            $"'{ship.ShipName}': " +
            $"desired heading={desiredHeading}, " +
            $"position=({ship.X:0.00}, " +
            $"{ship.Y:0.00}).");

        // -------------------------------------------------------------
        // Continue an existing avoidance route if possible.
        // -------------------------------------------------------------

        if (TryGetAvoidanceHeading(
                ship,
                out Heading avoidanceHeading))
        {
            if (!IsHeadingBlocked(
                    ship,
                    avoidanceHeading,
                    lookAheadDistance))
            {
                Log.Info(
                    $"Navigation continuing obstacle " +
                    $"avoidance: " +
                    $"Ship={ship.ShipName}, " +
                    $"Heading={avoidanceHeading}");

                return avoidanceHeading;
            }

            Log.Info(
                $"Navigation avoidance heading blocked: " +
                $"Ship={ship.ShipName}, " +
                $"Heading={avoidanceHeading}");

            ClearAvoidanceHeading(
                ship);
        }

        // -------------------------------------------------------------
        // Find a new avoidance heading.
        // -------------------------------------------------------------

        Heading[] candidates =
            GetHeadingCandidates(
                desiredHeading);

        Heading bestHeading =
            ship.Heading;

        float bestDistance =
            float.MaxValue;

        bool foundClearHeading =
            false;

        foreach (Heading candidate
            in candidates)
        {
            if (IsHeadingBlocked(
                    ship,
                    candidate,
                    lookAheadDistance))
            {
                continue;
            }

            GetNextPosition(
                ship,
                candidate,
                lookAheadDistance,
                out float newX,
                out float newY);

           ShipNavigationWaypoint? waypoint =
    GetCurrentWaypoint(ship);

float targetX;
float targetY;

if (waypoint != null &&
    !string.Equals(
        waypoint.AreaResRef,
        ship.AreaResRef,
        StringComparison.OrdinalIgnoreCase) &&
    _sailingAreaService.TryGetArea(
        ship.AreaResRef,
        out SailingArea? area))
{
    targetX = newX;
    targetY = newY;

    if (string.Equals(
            area.EastAreaResRef,
            waypoint.AreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        targetX = area.MaxX;
    }
    else if (string.Equals(
                 area.WestAreaResRef,
                 waypoint.AreaResRef,
                 StringComparison.OrdinalIgnoreCase))
    {
        targetX = area.MinX;
    }
    else if (string.Equals(
                 area.NorthAreaResRef,
                 waypoint.AreaResRef,
                 StringComparison.OrdinalIgnoreCase))
    {
        targetY = area.MaxY;
    }
    else if (string.Equals(
                 area.SouthAreaResRef,
                 waypoint.AreaResRef,
                 StringComparison.OrdinalIgnoreCase))
    {
        targetY = area.MinY;
    }
}
else
{
    targetX =
        waypoint?.X ??
        ship.DestinationX;

    targetY =
        waypoint?.Y ??
        ship.DestinationY;
}

float deltaX =
    targetX - newX;

float deltaY =
    targetY - newY;

float distance =
    MathF.Sqrt(
        deltaX * deltaX +
        deltaY * deltaY);
        }
            // -------------------------------------------------------------
            // Store selected avoidance heading.
            // -------------------------------------------------------------

            if (foundClearHeading)
        {
            if (bestHeading !=
                desiredHeading)
            {
                SetAvoidanceHeading(
                    ship,
                    bestHeading);

                Log.Info(
                    $"Navigation avoiding obstacle: " +
                    $"Ship={ship.ShipName}, " +
                    $"Desired={desiredHeading}, " +
                    $"Selected={bestHeading}");
            }

            return bestHeading;
        }

        // -------------------------------------------------------------
        // Completely blocked.
        // -------------------------------------------------------------

        Log.Warn(
            $"No clear navigation heading found for " +
            $"ship '{ship.ShipName}'. " +
            $"Remaining on heading {ship.Heading}.");

        return ship.Heading;
    }

    // -----------------------------------------------------------------
    // Temporary Avoidance State
    // -----------------------------------------------------------------

    private void SetAvoidanceHeading(
        ShipState ship,
        Heading heading)
    {
        _avoidanceHeadings[
            ship.ShipName] =
            heading;
    }

    private void ClearAvoidanceHeading(
        ShipState ship)
    {
        _avoidanceHeadings.Remove(
            ship.ShipName);
    }

    private bool TryGetAvoidanceHeading(
        ShipState ship,
        out Heading heading)
    {
        return _avoidanceHeadings.TryGetValue(
            ship.ShipName,
            out heading);
    }

    // -----------------------------------------------------------------
    // Obstacle Checks
    // -----------------------------------------------------------------

    public bool IsHeadingBlocked(
        ShipState ship,
        Heading heading,
        float distance)
    {
        GetNextPosition(
            ship,
            heading,
            distance,
            out float newX,
            out float newY);

        return IsPositionBlocked(
            ship,
            newX,
            newY);
    }

    public bool IsPositionBlocked(
        ShipState ship,
        float x,
        float y)
    {
        return _shipObstacleService.GetObstacleAt(
                   ship.AreaResRef,
                   x,
                   y) != null;
    }

    // -----------------------------------------------------------------
    // Heading Candidates
    // -----------------------------------------------------------------

    private static Heading[] GetHeadingCandidates(
        Heading desiredHeading)
    {
        int desiredIndex =
            GetHeadingIndex(
                desiredHeading);

        return new[]
        {
            GetHeadingFromIndex(
                desiredIndex),

            GetHeadingFromIndex(
                desiredIndex + 1),

            GetHeadingFromIndex(
                desiredIndex - 1),

            GetHeadingFromIndex(
                desiredIndex + 2),

            GetHeadingFromIndex(
                desiredIndex - 2),

            GetHeadingFromIndex(
                desiredIndex + 3),

            GetHeadingFromIndex(
                desiredIndex - 3),

            GetHeadingFromIndex(
                desiredIndex + 4)
        };
    }

    // -----------------------------------------------------------------
    // Position Calculation
    // -----------------------------------------------------------------

    private static void GetNextPosition(
        ShipState ship,
        Heading heading,
        float distance,
        out float newX,
        out float newY)
    {
        newX =
            ship.X;

        newY =
            ship.Y;

        switch (heading)
        {
            case Heading.North:
                newY += distance;
                break;

            case Heading.NorthEast:
                newX += distance;
                newY += distance;
                break;

            case Heading.East:
                newX += distance;
                break;

            case Heading.SouthEast:
                newX += distance;
                newY -= distance;
                break;

            case Heading.South:
                newY -= distance;
                break;

            case Heading.SouthWest:
                newX -= distance;
                newY -= distance;
                break;

            case Heading.West:
                newX -= distance;
                break;

            case Heading.NorthWest:
                newX -= distance;
                newY += distance;
                break;
        }
    }

    // -----------------------------------------------------------------
    // Heading Index
    // -----------------------------------------------------------------

    private static int GetHeadingIndex(
        Heading heading)
    {
        return heading switch
        {
            Heading.North =>
                0,

            Heading.NorthEast =>
                1,

            Heading.East =>
                2,

            Heading.SouthEast =>
                3,

            Heading.South =>
                4,

            Heading.SouthWest =>
                5,

            Heading.West =>
                6,

            Heading.NorthWest =>
                7,

            _ =>
                0
        };
    }

    private static Heading GetHeadingFromIndex(
        int index)
    {
        index %= 8;

        if (index < 0)
        {
            index += 8;
        }

        return index switch
        {
            0 =>
                Heading.North,

            1 =>
                Heading.NorthEast,

            2 =>
                Heading.East,

            3 =>
                Heading.SouthEast,

            4 =>
                Heading.South,

            5 =>
                Heading.SouthWest,

            6 =>
                Heading.West,

            7 =>
                Heading.NorthWest,

            _ =>
                Heading.North
        };
    }

    // -----------------------------------------------------------------
    // Destination
    // -----------------------------------------------------------------

    public bool IsDestinationReached(
        ShipState ship)
    {
        if (!IsNavigating(
                ship))
        {
            return false;
        }

        if (!string.Equals(
                ship.AreaResRef,
                ship.DestinationAreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        float deltaX =
            ship.DestinationX -
            ship.X;

        float deltaY =
            ship.DestinationY -
            ship.Y;

        float distance =
            MathF.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY);

        return distance <=
               DestinationThreshold;
    }

    public float GetDistanceToDestination(
        ShipState ship)
    {
        if (!IsNavigating(
                ship))
        {
            return 0.0f;
        }

        if (!string.Equals(
                ship.AreaResRef,
                ship.DestinationAreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            return float.MaxValue;
        }

        float deltaX =
            ship.DestinationX -
            ship.X;

        float deltaY =
            ship.DestinationY -
            ship.Y;

        return MathF.Sqrt(
            deltaX * deltaX +
            deltaY * deltaY);
    }

    // -----------------------------------------------------------------
    // Complete Navigation
    // -----------------------------------------------------------------

    public void CompleteNavigation(
        ShipState ship)
    {
        if (!IsNavigating(
                ship))
        {
            return;
        }

        Log.Info(
            $"Navigation destination reached: " +
            $"Ship={ship.ShipName}, " +
            $"Area={ship.AreaResRef}, " +
            $"X={ship.X:0.00}, " +
            $"Y={ship.Y:0.00}");

        ClearDestination(
            ship);
    }
    public bool IsNextWaypointInAnotherArea(
    ShipState ship)
{
    if (!_routes.TryGetValue(
            ship.ShipName,
            out ShipNavigationRoute? route))
    {
        return false;
    }

    int nextIndex =
        route.CurrentWaypointIndex + 1;

    if (nextIndex >= route.Waypoints.Count)
    {
        return false;
    }

    return !string.Equals(
        route.Waypoints[nextIndex].AreaResRef,
        ship.AreaResRef,
        StringComparison.OrdinalIgnoreCase);
}
}