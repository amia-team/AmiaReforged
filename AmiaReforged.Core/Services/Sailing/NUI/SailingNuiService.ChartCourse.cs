using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Newtonsoft.Json;

namespace AmiaReforged.Core.Services.Sailing;

// Mouse payload uses Anvil's Newtonsoft-based JsonUtility, not System.Text.Json.
public sealed class ChartMousePayload
{
    [JsonProperty("mouse_btn")] public int? Button { get; init; }
    [JsonProperty("mouse_pos")] public ChartMousePosition? Position { get; init; }
}
public sealed class ChartMousePosition
{
    [JsonProperty("x")] public float? X { get; init; }
    [JsonProperty("y")] public float? Y { get; init; }
}

public partial class SailingNuiService
{
    public const string ChartSurfaceId = "sailing_chart_surface";
    private readonly ShipRoutePlannerService _chartPlanner;
    private readonly PhysicalShipService _chartPhysical;
    private readonly Dictionary<string, ChartCourseSession> _courseSessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (ShipState Ship, string Area, ChartViewport Viewport)> _coursePresses = new(StringComparer.Ordinal);
    private readonly NuiBind<string> _coursePreviewBind = new("chart_course_preview");
    private readonly NuiBind<bool> _coursePreviewEnabled = new("chart_course_preview_enabled");

    private bool CourseAuthorized(NwPlayer player, ShipState ship) => player.IsValid && _registry.IsCurrent(ship) &&
        ship.HelmsmanPCKey == player.PlayerName && _chartPhysical.IsPlayerPresent(ship.ShipName, player) &&
        !_chartPhysical.IsTransferPending(player.PlayerName) && _chartPhysical.CanCommandShip(ship, player);

    private bool HandleCourseEvent(ModuleEvents.OnNuiEvent evt)
    {
        if (!OwnsEvent(evt) || !_chartShips.TryGetValue(evt.Player.PlayerName, out var ship) ||
            !_courseSessions.TryGetValue(evt.Player.PlayerName, out var session)) return false;
        string viewer = evt.Player.PlayerName;
        bool surface = evt.ElementId == ChartSurfaceId;
        if (evt.EventType == NuiEventType.MouseDown && surface)
        {
            _coursePresses.Remove(viewer);
            var payload = MousePayload(evt);
            if (payload?.Button == 0 && session.CanSelect(ship, CourseAuthorized(evt.Player, ship)) &&
                _charts.TryGet(viewer, out var chart) && chart.Viewport is { } viewport)
                _coursePresses[viewer] = (ship, ship.AreaResRef, viewport);
            else evt.Player.SendServerMessage("You must control the helm aboard an active ship in ocean_01 to plot a course.");
            return true;
        }
        if (evt.EventType == NuiEventType.MouseUp)
        {
            bool pressed = _coursePresses.Remove(viewer, out var press);
            if (surface)
            {
                var payload = MousePayload(evt);
                if (payload?.Button != 0 || !pressed || !ReferenceEquals(press.Ship, ship) ||
                    payload.Position?.X is not { } x || payload.Position.Y is not { } y) return true;
                if (!_charts.TryGet(viewer, out var chart) || chart.Viewport != press.Viewport || ship.AreaResRef != press.Area)
                { evt.Player.SendServerMessage("The chart moved while clicking. Click again to choose a destination."); return true; }
                session.Select(_registry.IsCurrent(ship) ? ship : null, CourseAuthorized(evt.Player, ship),
                    x, y, press.Viewport, out string message);
                evt.Player.SendServerMessage(message);
                Update(evt.Player, ship);
                return true;
            }
        }
        if (evt.EventType != NuiEventType.Click || evt.ElementId is not ("chart_confirm" or "chart_cancel")) return false;
        if (evt.ElementId == "chart_cancel") session.CancelPreview();
        else
        {
            session.Confirm(_registry.IsCurrent(ship) ? ship : null, CourseAuthorized(evt.Player, ship), out string message);
            evt.Player.SendServerMessage(message);
        }
        Update(evt.Player, ship);
        return true;
    }

    private static ChartMousePayload? MousePayload(ModuleEvents.OnNuiEvent evt)
    {
        try { return evt.GetEventPayload<ChartMousePayload>(); }
        catch (JsonException) { return null; }
    }

    private NauticalChartFrame ApplyCoursePreview(NwPlayer player, ShipState ship, NuiWindowToken token, NauticalChartFrame frame)
    {
        _courseSessions.TryGetValue(player.PlayerName, out var session);
        if (session != null && !session.CanSelect(ship, CourseAuthorized(player, ship))) session.CancelPreview();
        var preview = session?.Preview;
        token.SetBindValue(_coursePreviewEnabled, preview != null);
        token.SetBindValue(_coursePreviewBind, preview == null
            ? _shipNavigationService.GetCourseFailure(ship) ?? "Click chart to preview a course"
            : FormattableString.Invariant($"Preview: {preview.Destination.X:0.#}, {preview.Destination.Y:0.#} | {preview.Route.Waypoints.Count} legs"));
        return preview == null ? frame : frame with
        {
            Preview = preview.Route.Waypoints.Select((w, i) =>
                new ChartRoutePoint(w.AreaResRef, w.X, w.Y, i == preview.Route.Waypoints.Count - 1)).ToArray()
        };
    }
}
