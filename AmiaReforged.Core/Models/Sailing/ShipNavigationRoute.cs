namespace AmiaReforged.Core.Models.Sailing;

public class ShipNavigationRoute
{
    public required string ShipName { get; set; }

    public List<ShipNavigationWaypoint>
        Waypoints { get; set; } =
            new();

    public int CurrentWaypointIndex { get; set; }

public bool Loop { get; set; }
    
   public bool IsComplete =>
    !Loop &&
    CurrentWaypointIndex >= Waypoints.Count;

    public ShipNavigationWaypoint?
        CurrentWaypoint
    {
        get
        {
            if (IsComplete)
            {
                return null;
            }

            return Waypoints[
                CurrentWaypointIndex];
        }
    }
}