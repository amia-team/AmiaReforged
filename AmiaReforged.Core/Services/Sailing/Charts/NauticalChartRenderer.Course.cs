using AmiaReforged.Core.Models.Sailing;
using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

public sealed partial class NauticalChartRenderer
{
    public const float CourseSpacing = 8;
    public const float CourseDashLength = 2;
    private ChartRoutePoint[] _courseRoute = [];
    private ChartPoint? _courseOrigin;
    private string? _courseArea, _courseShip;
    private ChartRouteSegment[] _courseWorld = [], _courseScreen = [];
    private ChartViewport? _courseCamera;
    private int _courseCapacity;
    public int CourseGeometryBuilds { get; private set; }

    private void PrepareCourse(NauticalChartFrame frame, ChartViewport viewport)
    {
        var own = frame.Markers.FirstOrDefault(m => m.Kind == ChartContactKind.OwnShip);
        ChartPoint? origin = own == null || frame.Route.Length == 0 ? null : new(own.X, own.Y);
        bool changed = _courseOrigin != origin || _courseArea != frame.Area || _courseShip != frame.ShipId ||
                       !_courseRoute.SequenceEqual(frame.Route);
        if (changed)
        {
            _courseOrigin = origin; _courseArea = frame.Area; _courseShip = frame.ShipId;
            _courseRoute = frame.Route.ToArray();
            _courseWorld = origin == null ? [] : CourseDashes(origin.Value, frame.Route, frame.Area);
            CourseGeometryBuilds++;
        }
        if (!changed && _courseCamera == viewport) return;
        _courseCamera = viewport;
        _courseScreen = _courseWorld.Select(s =>
        {
            bool on = NauticalChartGeometry.ClipLine(viewport.Project(s.From.X, s.From.Y),
                viewport.Project(s.To.X, s.To.Y), viewport.Size, out var line);
            return (On: on, Line: line);
        }).Where(s => s.On).Select(s => s.Line).ToArray();
        if (frame.Route.Length == 0 || origin == null) _courseCapacity = 0;
        else
        {
            // Grow in buckets; moving or zooming back out never makes the drawing tree oscillate.
            int needed = 16; while (needed < _courseWorld.Length) needed *= 2;
            _courseCapacity = Math.Max(_courseCapacity, needed);
        }
    }

    /// <summary>Sample only actual remaining legs, with a continuous world-space cadence anchored at the last waypoint.</summary>
    public static ChartRouteSegment[] CourseDashes(ChartPoint origin, ChartRoutePoint[] route, string area)
    {
        var local = route.TakeWhile(p => string.Equals(p.Area, area, StringComparison.OrdinalIgnoreCase) &&
            float.IsFinite(p.X) && float.IsFinite(p.Y)).ToArray();
        if (!float.IsFinite(origin.X) || !float.IsFinite(origin.Y)) return [];
        var result = new List<ChartRouteSegment>(); double tail = 0;
        for (int i = local.Length - 1; i >= 0; i--)
        {
            var end = new ChartPoint(local[i].X, local[i].Y);
            var start = i == 0 ? origin : new ChartPoint(local[i - 1].X, local[i - 1].Y);
            double dx = start.X - end.X, dy = start.Y - end.Y, length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= .00001) continue;
            ChartPoint At(double d) => new(end.X + (float)(dx * d / length), end.Y + (float)(dy * d / length));
            for (double dash = Math.Floor(tail / CourseSpacing) * CourseSpacing; dash < tail + length; dash += CourseSpacing)
            {
                double from = Math.Max(dash, tail), to = Math.Min(dash + CourseDashLength, tail + length);
                if (to - from > .00001) result.Add(new(At(to - tail), At(from - tail)));
            }
            tail += length;
        }
        result.Reverse();
        return result.ToArray();
    }

    private void CourseValues(Dictionary<string, object> values)
    {
        for (int i = 0; i < _courseCapacity; i++)
        {
            string key = $"chart_route{i}";
            values[key + "_on"] = i < _courseScreen.Length;
            if (i >= _courseScreen.Length) continue;
            values[key + "_a"] = _courseScreen[i].From; values[key + "_b"] = _courseScreen[i].To;
        }
    }

    private void AddCourse(List<NuiDrawListItem> drawings)
    {
        for (int i = 0; i < _courseCapacity; i++)
            ChartOverlayDrawing.AddBackedLine(drawings, $"chart_route{i}", ChartOverlayDrawing.Course, 2.2f);
    }
}
