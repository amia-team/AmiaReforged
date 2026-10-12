using AmiaReforged.Core.Models.Sailing;
using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

public sealed record ChartBindChange(string Key, object Value);
public sealed record ChartRenderUpdate(NuiRow? Layout, IReadOnlyList<ChartBindChange> Changes);

/// <summary>Presentation lifetime is per viewer/token, even when several viewers share a vessel.</summary>
public sealed class NauticalChartWindowCache
{
    private readonly Dictionary<string, NauticalChartRenderer> _windows = new(StringComparer.Ordinal);
    public NauticalChartRenderer Open(string viewer) => _windows[viewer] = new();
    public bool TryGet(string viewer, out NauticalChartRenderer renderer) => _windows.TryGetValue(viewer, out renderer!);
    public void Close(string viewer) => _windows.Remove(viewer);
    public void Clear() => _windows.Clear();
    public int Count => _windows.Count;
}

/// <summary>One cache per window. Movement/heading/fog update binds, not the static drawing tree.</summary>
public sealed class NauticalChartRenderer
{
    private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
    private NauticalChartFrame? _frame;
    private float _size;
    private string[] _topology = [];
    public int LayoutBuilds { get; private set; }
    public int BindWrites { get; private set; }
    private static Color Rgb(byte r, byte g, byte b) => new(r, g, b, (byte)255);
    private static readonly Color Ink = Rgb(17, 29, 38), FogInk = Rgb(38, 54, 65), Parchment = Rgb(241, 219, 165), RouteInk = Rgb(220, 184, 108);
    public static Color MarkerColor(ChartContactKind kind) => kind switch
    {
        ChartContactKind.OwnShip => Rgb(255, 219, 126), ChartContactKind.Player => Rgb(132, 211, 239),
        ChartContactKind.Merchant => Rgb(142, 222, 158), ChartContactKind.Pirate => Rgb(240, 111, 101), _ => Rgb(179, 183, 186)
    };
    public static float MarkerSize(ChartContactKind kind) => kind switch
    { ChartContactKind.OwnShip => 40, ChartContactKind.Player => 28, ChartContactKind.Merchant => 30, ChartContactKind.Pirate => 32, _ => 20 };

    public ChartRenderUpdate Prepare(NauticalChartFrame frame, float size)
    {
        var segments = NauticalChartProjection.Segments(frame, size);
        var topology = frame.Markers.Select(m => $"{m.Id}:{m.Kind}:{m.Sprite.Length > 0}:{m.Heading != null}")
            .Concat(frame.Features.Select(f => f.ToString()))
            .Append($"route:{segments.Length}:{frame.Route.Length}").ToArray();
        bool rebuild = _frame == null || _size != size || _frame.Area != frame.Area || _frame.ShipId != frame.ShipId || !_topology.SequenceEqual(topology);
        if (rebuild) { _values.Clear(); LayoutBuilds++; }
        var values = Values(frame, size, segments);
        var changes = new List<ChartBindChange>();
        foreach (var pair in values)
            if (!_values.TryGetValue(pair.Key, out var old) || !Equals(old, pair.Value))
            { changes.Add(new(pair.Key, pair.Value)); _values[pair.Key] = pair.Value; }
        BindWrites += changes.Count;
        _frame = frame; _size = size; _topology = topology;
        return new(rebuild ? CreateLayout(frame, size, segments.Length) : null, changes);
    }

    private static Dictionary<string, object> Values(NauticalChartFrame frame, float size, ChartRouteSegment[] segments)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        result["ship_heading"] = frame.Heading; result["chart_motion"] = frame.Motion;
        result["chart_mode"] = frame.Mode; result["chart_destination"] = frame.Destination; result["chart_waypoints"] = frame.Waypoints;
        var destinationLines = frame.Destination.Split('\n');
        result["chart_next"] = destinationLines[0];
        result["chart_final"] = destinationLines.Length > 1 ? destinationLines[1] : "Final destination: --";
        result["chart_contacts"] = $"Visible chart contacts: {frame.Markers.Count(m => m.Kind != ChartContactKind.OwnShip)}";
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) result[$"chart_f{x}_{y}"] = frame.Discovery[x * 16 + y] != '1';
        for (int i = 0; i < segments.Length; i++)
        { result[$"chart_r{i}_a"] = segments[i].From; result[$"chart_r{i}_b"] = segments[i].To; }
        for (int i = 0; i < frame.Route.Length; i++)
        {
            var point = frame.Route[i]; var cell = NauticalChartGeometry.Cell(point.X, point.Y);
            result[$"chart_w{i}_rect"] = NauticalChartGeometry.CenteredMarker(point.X, point.Y, point.Final ? 14 : 10, point.Final ? 14 : 10, size);
            result[$"chart_w{i}_on"] = NauticalChartGeometry.InWorld(point.X, point.Y) && frame.Discovery[cell.X * 16 + cell.Y] == '1';
            result[$"chart_w{i}_final"] = point.Final && (bool)result[$"chart_w{i}_on"];
        }
        for (int i = 0; i < frame.Markers.Length; i++)
        {
            var marker = frame.Markers[i]; float diameter = MarkerSize(marker.Kind);
            var rect = NauticalChartGeometry.CenteredMarker(marker.X, marker.Y, diameter, diameter, size);
            result[$"chart_m{i}_ring"] = rect;
            result[$"chart_m{i}_sprite"] = marker.Sprite;
            result[$"chart_m{i}_image"] = new ChartRect(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
            result[$"chart_m{i}_dot"] = new ChartRect(rect.X + diameter / 2 - 3, rect.Y + diameter / 2 - 3, 6, 6);
            result[$"chart_m{i}_selected"] = marker.Selected;
            if (marker.Heading is { } heading)
            {
                var direction = NauticalChartGeometry.HeadingVector(heading);
                var center = new ChartPoint(rect.X + diameter / 2, rect.Y + diameter / 2);
                var tip = new ChartPoint(center.X + direction.X * (diameter / 2 + 10), center.Y + direction.Y * (diameter / 2 + 10));
                NauticalChartGeometry.ClipLine(center, tip, size, out var beam);
                result[$"chart_m{i}_a"] = beam.From; result[$"chart_m{i}_b"] = beam.To;
            }
        }
        return result;
    }

    private static NuiRow CreateLayout(NauticalChartFrame frame, float size, int segmentCount)
    {
        List<NuiDrawListItem> drawings = [new NuiDrawListImage("sailing_map", new NuiRect(0, 0, size, size))
            { Aspect = NuiAspect.Stretch, HorizontalAlign = NuiHAlign.Left, VerticalAlign = NuiVAlign.Top }];
        // Geographic artwork retains its existing sprite references and is masked by fog.
        foreach (var feature in frame.Features.Where(f => !f.Port && f.Sprite.Length > 0))
        {
            var rect = NauticalChartGeometry.Artwork(feature.X, feature.Y, feature.Width, feature.Height, size);
            drawings.Add(new NuiDrawListImage(feature.Sprite, Rect(rect))
                { Aspect = NuiAspect.Fit, HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle });
        }
        // Routes are underneath fog. Planning does not reveal terrain, markers or contacts.
        for (int i = 0; i < segmentCount; i++)
            drawings.Add(new NuiDrawListLine(RouteInk, false, 1.5f, new NuiBind<NuiVector>($"chart_r{i}_a"), new NuiBind<NuiVector>($"chart_r{i}_b")));
        for (int i = 0; i < frame.Route.Length; i++)
        {
            var rect = new NuiBind<NuiRect>($"chart_w{i}_rect");
            drawings.Add(new NuiDrawListCircle(RouteInk, false, 1.5f, rect) { Enabled = new NuiBind<bool>($"chart_w{i}_on") });
            drawings.Add(new NuiDrawListCircle(Parchment, true, 1, rect)
            { Enabled = new NuiBind<bool>($"chart_w{i}_final") });
        }
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
        {
            var cell = NauticalChartGeometry.FogCell(x, y, size); var enabled = new NuiBind<bool>($"chart_f{x}_{y}");
            // A single flat color, with no tile artwork or stroke, preserves the discovery mask.
            drawings.Add(new NuiDrawListPolyLine(FogInk, true, 0,
                [cell.X, cell.Y, cell.X + cell.Width, cell.Y, cell.X + cell.Width, cell.Y + cell.Height,
                 cell.X, cell.Y + cell.Height, cell.X, cell.Y]) { Enabled = enabled });
        }
        var labels = new List<ChartRect>();
        foreach (var feature in frame.Features)
        {
            var rect = NauticalChartGeometry.CenteredMarker(feature.X, feature.Y, 12, 12, size);
            // Ports are diamonds; geographic markers use an outlined circle as a texture-independent fallback.
            if (feature.Port)
                drawings.Add(new NuiDrawListPolyLine(Parchment, false, 1.5f,
                    [rect.X + 6, rect.Y, rect.X + 12, rect.Y + 6, rect.X + 6, rect.Y + 12, rect.X, rect.Y + 6, rect.X + 6, rect.Y]));
            else drawings.Add(new NuiDrawListCircle(Parchment, false, 1, Rect(rect)));
            if (!feature.Port) continue;
            var candidates = new[] { new ChartRect(rect.X + 16, rect.Y - 8, 116, 22), new ChartRect(rect.X - 120, rect.Y - 8, 116, 22),
                new ChartRect(rect.X - 20, rect.Y - 26, 116, 22), new ChartRect(rect.X - 20, rect.Y + 16, 116, 22) };
            var label = candidates.FirstOrDefault(r => r.X >= 0 && r.Y >= 0 && r.X + r.Width <= size && r.Y + r.Height <= size &&
                labels.All(l => !Overlaps(l, r)) && frame.Markers.All(m => !Overlaps(NauticalChartGeometry.Marker(m.X, m.Y, MarkerSize(m.Kind), MarkerSize(m.Kind), size), r)));
            if (label.Width == 0) continue; // Avoid covering ships or crowded geographic labels.
            labels.Add(label); drawings.Add(new NuiDrawListText(Parchment, Rect(label), feature.Name));
        }
        // Compass stays underneath ships so a vessel at the northeast boundary has foreground priority.
        drawings.Add(new NuiDrawListText(Parchment, new NuiRect(size - 28, 6, 22, 24), "N"));
        drawings.Add(new NuiDrawListLine(Parchment, false, 1, new NuiVector(size - 18, 30), new NuiVector(size - 18, 48)));
        drawings.Add(new NuiDrawListLine(Parchment, false, 1, new NuiVector(size - 18, 30), new NuiVector(size - 23, 37)));
        drawings.Add(new NuiDrawListLine(Parchment, false, 1, new NuiVector(size - 18, 30), new NuiVector(size - 13, 37)));
        // Contact order is stable by classification/identity; own ship is always the final moving marker.
        for (int i = 0; i < frame.Markers.Length; i++)
        {
            var marker = frame.Markers[i]; var color = MarkerColor(marker.Kind); var ring = new NuiBind<NuiRect>($"chart_m{i}_ring");
            drawings.Add(new NuiDrawListCircle(Ink, true, 0, ring));
            drawings.Add(new NuiDrawListCircle(color, false, marker.Kind == ChartContactKind.OwnShip ? 2.5f : 1.5f, ring));
            drawings.Add(new NuiDrawListCircle(color, true, 0, new NuiBind<NuiRect>($"chart_m{i}_dot")));
            if (marker.Heading != null)
                drawings.Add(new NuiDrawListLine(color, false, marker.Kind == ChartContactKind.OwnShip ? 2 : 1,
                    new NuiBind<NuiVector>($"chart_m{i}_a"), new NuiBind<NuiVector>($"chart_m{i}_b")));
            if (marker.Sprite.Length > 0)
                drawings.Add(new NuiDrawListImage(new NuiBind<string>($"chart_m{i}_sprite"), new NuiBind<NuiRect>($"chart_m{i}_image"))
                    { Aspect = NuiAspect.Fit, HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle });
            drawings.Add(new NuiDrawListCircle(Parchment, false, 1, ring) { Enabled = new NuiBind<bool>($"chart_m{i}_selected") });
        }
        return new NuiRow { Width = size, Height = size, Margin = 0, Padding = 0, Scissor = true, DrawList = drawings };
    }
    private static bool Overlaps(ChartRect a, ChartRect b) => a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
    private static NuiRect Rect(ChartRect r) => new(r.X, r.Y, r.Width, r.Height);
    public static void Publish(NuiWindowToken token, IEnumerable<ChartBindChange> changes)
    {
        foreach (var change in changes)
            switch (change.Value)
            {
                case bool value: token.SetBindValue(new NuiBind<bool>(change.Key), value); break;
                case string value: token.SetBindValue(new NuiBind<string>(change.Key), value); break;
                case ChartRect value: token.SetBindValue(new NuiBind<NuiRect>(change.Key), Rect(value)); break;
                case ChartPoint value: token.SetBindValue(new NuiBind<NuiVector>(change.Key), new NuiVector(value.X, value.Y)); break;
            }
    }
}
