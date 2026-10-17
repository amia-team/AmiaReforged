namespace AmiaReforged.Core.Models.Sailing;

public class ShipNavigationRoute
{
    public required string ShipName { get; set; }

    public List<ShipNavigationWaypoint>
        Waypoints { get; set; } =
            new();

    public int CurrentWaypointIndex { get; set; }

    // Local chart courses follow validated legs and stop if a leg becomes blocked.
    // Serialized with the existing route payload; older routes default to false.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsChartCourse { get; set; }

public bool Loop { get; set; }
    
   public bool IsComplete =>
    Waypoints.Count == 0 || (!Loop && CurrentWaypointIndex >= Waypoints.Count);

    public ShipNavigationWaypoint?
        CurrentWaypoint
    {
        get
        {
            if (IsComplete || CurrentWaypointIndex < 0 || CurrentWaypointIndex >= Waypoints.Count)
            {
                return null;
            }

            return Waypoints[
                CurrentWaypointIndex];
        }
    }
}
