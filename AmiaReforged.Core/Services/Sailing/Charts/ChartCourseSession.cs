using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public sealed record ChartCoursePreview(ChartPoint Destination, ShipNavigationRoute Route);

/// <summary>Ephemeral per-window selection. Only Confirm installs a route in ShipNavigationService.</summary>
public sealed class ChartCourseSession(string viewer, ShipState capturedShip,
    ShipRoutePlannerService planner, ShipNavigationService navigation, ShipObstacleService obstacles)
{
    public ChartCoursePreview? Preview { get; private set; }
    public bool Closed { get; private set; }

    public bool CanSelect(ShipState? current, bool presentAndAuthorized) => !Closed &&
        ReferenceEquals(current, capturedShip) && presentAndAuthorized && capturedShip.HelmsmanPCKey == viewer &&
        capturedShip.ShipType == ShipType.Player && capturedShip.IsActiveWorld && !capturedShip.IsDisabled &&
        !capturedShip.IsInPort && !capturedShip.IsEconomyBusy &&
        string.Equals(capturedShip.AreaResRef, NauticalChartGeometry.OceanArea, StringComparison.OrdinalIgnoreCase);

    public bool Select(ShipState? current, bool presentAndAuthorized, float x, float y, float size, out string message)
        => Select(current, presentAndAuthorized, x, y, ChartViewport.Create(size), out message);

    public bool Select(ShipState? current, bool presentAndAuthorized, float x, float y, ChartViewport viewport, out string message)
    {
        Preview = null; // A second click always replaces the previous proposal, including invalid clicks.
        if (!CanSelect(current, presentAndAuthorized))
        { message = "You must control this ship's helm aboard an active vessel in ocean_01."; return false; }
        if (!viewport.TryUnproject(x, y, out var destination))
        { message = "Choose a point inside the nautical chart."; return false; }
        if (!obstacles.CanMoveTo(capturedShip.AreaResRef, destination.X, destination.Y))
        { message = "That destination is on configured land or a blocked position."; return false; }
        var route = planner.BuildChartRoute(capturedShip, destination, obstacles);
        if (route == null)
        { message = "No safe ocean course could be found to that destination."; return false; }
        Preview = new(destination, route);
        message = "Course preview ready. Confirm course to engage autopilot.";
        return true;
    }

    public bool Confirm(ShipState? current, bool presentAndAuthorized, out string message)
    {
        var proposal = Preview;
        if (!CanSelect(current, presentAndAuthorized))
        { Preview = null; message = "Course rejected: you no longer control this ship's helm in ocean_01."; return false; }
        if (proposal == null) { message = "Select a destination on the chart first."; return false; }
        // Replan once from the current ship position. A moving ship or changed obstacle configuration
        // must never engage the stale starting leg that was displayed by the preview.
        var route = planner.BuildChartRoute(capturedShip, proposal.Destination, obstacles);
        Preview = null;
        if (route == null) { message = "Course rejected: the destination or route is now blocked."; return false; }
        navigation.SetRoute(capturedShip, route);
        message = "Autopilot engaged.";
        return true;
    }

    public void CancelPreview() => Preview = null;
    public void Close() { Preview = null; Closed = true; }
}
