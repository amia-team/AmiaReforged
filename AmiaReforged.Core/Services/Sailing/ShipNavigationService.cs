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
    ShipState,
    ShipNavigationRoute>
    _routes = new();

    private readonly Dictionary<
        ShipState,
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
        _routes[ship] = route;

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

        //Log.Info(
        //$"Navigation route set: " +
        //$"Ship={ship}, " +
        //$"Waypoints={route.Waypoints.Count}");
    }

    public void PruneRemovedShips(IEnumerable<ShipState> fleet)
    {
        HashSet<ShipState> current = fleet.ToHashSet();
        foreach (ShipState ship in _routes.Keys.Where(s => !current.Contains(s)).ToArray()) _routes.Remove(ship);
        foreach (ShipState ship in _avoidanceHeadings.Keys.Where(s => !current.Contains(s)).ToArray()) _avoidanceHeadings.Remove(ship);
    }

    public void CancelNavigation(ShipState ship)
    {
        ClearRoute(ship);
        ClearDestination(ship);
        ship.Underway = false;
    }

    public void ClearRoute(
        ShipState ship)
    {
        if (_routes.Remove(
                ship))
        {
            //Log.Info(
            //$"Navigation route cleared: " +
            //$"Ship={ship}");
        }
    }

    public ShipNavigationRoute?
    GetRoute(
        ShipState ship)
    {
        if (_routes.TryGetValue(
                ship,
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

        if (route.IsComplete) return true;

        ShipNavigationWaypoint? waypoint =
            route.CurrentWaypoint;

        //Log.Info(
        //$"Navigation waypoint reached: " +
        //$"Ship={ship}, " +
        //$"Waypoint={route.CurrentWaypointIndex}, " +
        //$"Area={waypoint?.AreaResRef}, " +
        //$"X={waypoint?.X:0.00}, " +
        //$"Y={waypoint?.Y:0.00}");

        route.CurrentWaypointIndex++;

        // -------------------------------------------------------------
        // Looping merchant route
        // -------------------------------------------------------------

        if (route.Loop &&
            route.CurrentWaypointIndex >= route.Waypoints.Count)
        {
            route.CurrentWaypointIndex = 0;

            //Log.Info(
            //$"Navigation route looping: " +
            //$"Ship={ship}");
        }

        // -------------------------------------------------------------
        // Non-looping route complete
        // -------------------------------------------------------------

        if (route.IsComplete)
        {
            //Log.Info(
            //$"Navigation route complete: " +
            //$"Ship={ship}");

            return true;
        }

        ShipNavigationWaypoint next = route.CurrentWaypoint!;
        SetDestination(ship, next.AreaResRef, next.X, next.Y, next.Z);

        //Log.Info(
        //$"Navigation advancing to waypoint: " +
        //$"Ship={ship}, " +
        //$"Waypoint={route.CurrentWaypointIndex}, " +
        //$"Area={next.AreaResRef}, " +
        //$"X={next.X:0.00}, " +
        //$"Y={next.Y:0.00}");

        return false;
    }    // -----------------------------------------------------------------
    // Destination
    // -----------------------------------------------------------------

    public bool SetDestination(
        ShipState ship,
        string destinationAreaResRef,
        float destinationX,
        float destinationY,
        float destinationZ)
    {
        if (ship.IsStored || ship.IsDisabled || !float.IsFinite(destinationX) || !float.IsFinite(destinationY) || !float.IsFinite(destinationZ) || string.IsNullOrWhiteSpace(
                destinationAreaResRef))
        {
            Log.Warn(
                $"Cannot set navigation destination for " +
                $"'{ship}': destination area is empty.");

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

        ship.IsNavigating = true;
        ship.Underway = true;

        //Log.Info(
        //$"Navigation destination set: " +
        //$"Ship={ship}, " +
        //$"Area={ship.DestinationAreaResRef}, " +
        //$"X={ship.DestinationX:0.00}, " +
        //$"Y={ship.DestinationY:0.00}, " +
        //$"Z={ship.DestinationZ:0.00}");

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

        //Log.Info(
        //$"Navigation destination cleared: " +
        //$"Ship={ship}");
    }

    public bool IsNavigating(
        ShipState ship)
    {
        return ship.IsActiveWorld && !ship.IsDisabled && ship.IsNavigating &&
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

        string? targetArea = waypoint?.AreaResRef ?? ship.DestinationAreaResRef;
        if (!string.IsNullOrEmpty(targetArea) &&
            !string.Equals(
                targetArea,
                ship.AreaResRef,
                StringComparison.OrdinalIgnoreCase))
        {
            if (_sailingAreaService.TryGetArea(
                    ship.AreaResRef,
                    out SailingArea? area) && area != null)
            {
                if (string.Equals(
                        area.EastAreaResRef,
                        targetArea,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Heading.East;
                }

                if (string.Equals(
                        area.WestAreaResRef,
                        targetArea,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Heading.West;
                }

                if (string.Equals(
                        area.NorthAreaResRef,
                        targetArea,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Heading.North;
                }

                if (string.Equals(
                        area.SouthAreaResRef,
                        targetArea,
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

        // Axis tolerances must agree with the radial arrival test. A target offset
        // by (0.8, 0.8) is outside radius 1, even though both axes are within 1.
        if (!east && !west && !north && !south &&
            deltaX * deltaX + deltaY * deltaY > DestinationThreshold * DestinationThreshold)
        {
            east = deltaX > 0; west = deltaX < 0;
            north = deltaY > 0; south = deltaY < 0;
        }

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

    public float GetMovementDistance(ShipState ship, Heading heading, float maximumDistance)
    {
        ShipNavigationWaypoint? waypoint = GetCurrentWaypoint(ship);
        string? area = waypoint?.AreaResRef ?? ship.DestinationAreaResRef;
        if (!string.Equals(area, ship.AreaResRef, StringComparison.OrdinalIgnoreCase) || GetDesiredHeading(ship) != heading)
            return maximumDistance; // Boundary travel and obstacle detours retain their configured speed.
        float dx = MathF.Abs((waypoint?.X ?? ship.DestinationX) - ship.X);
        float dy = MathF.Abs((waypoint?.Y ?? ship.DestinationY) - ship.Y);
        float remaining = heading switch
        {
            Heading.North or Heading.South => dy,
            Heading.East or Heading.West => dx,
            _ => MathF.Min(dx, dy)
        };
        return MathF.Min(maximumDistance, remaining);
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
                //Log.Info(
                //$"Navigation obstacle cleared: " +
                //$"Ship={ship}, " +
                //$"Avoidance={previousAvoidance}, " +
                //$"Resuming={desiredHeading}");

                ClearAvoidanceHeading(
                    ship);
            }

            return desiredHeading;
        }

        // -------------------------------------------------------------
        // Direct route is blocked.
        // -------------------------------------------------------------

        //Log.Info(
        //$"Navigation obstacle detected for " +
        //$"'{ship}': " +
        //$"desired heading={desiredHeading}, " +
        //$"position=({ship.X:0.00}, " +
        //$"{ship.Y:0.00}).");

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
                //Log.Info(
                //$"Navigation continuing obstacle " +
                //$"avoidance: " +
                //$"Ship={ship}, " +
                //$"Heading={avoidanceHeading}");

                return avoidanceHeading;
            }

            //Log.Info(
            //$"Navigation avoidance heading blocked: " +
            //$"Ship={ship}, " +
            //$"Heading={avoidanceHeading}");

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


        bool foundClearHeading = false;
        float bestDistance = float.MaxValue;

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
                    out SailingArea? area) && area != null)
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

            float distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestHeading = candidate;
                foundClearHeading = true;
            }
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

                //Log.Info(
                //$"Navigation avoiding obstacle: " +
                //$"Ship={ship}, " +
                //$"Desired={desiredHeading}, " +
                //$"Selected={bestHeading}");
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
            ship] =
            heading;
    }

    private void ClearAvoidanceHeading(
        ShipState ship)
    {
        _avoidanceHeadings.Remove(
            ship);
    }

    private bool TryGetAvoidanceHeading(
        ShipState ship,
        out Heading heading)
    {
        return _avoidanceHeadings.TryGetValue(
            ship,
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

        //Log.Info(
        //$"Navigation destination reached: " +
        //$"Ship={ship}, " +
        //$"Area={ship.AreaResRef}, " +
        //$"X={ship.X:0.00}, " +
        //$"Y={ship.Y:0.00}");

        ClearDestination(
            ship);
    }
    public bool IsNextWaypointInAnotherArea(
    ShipState ship)
    {
        if (!_routes.TryGetValue(
                ship,
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
