namespace AmiaReforged.Core.Models.Sailing;

public class ShipNavigationWaypoint
{
    public required string AreaResRef { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public string? Description { get; set; }
}