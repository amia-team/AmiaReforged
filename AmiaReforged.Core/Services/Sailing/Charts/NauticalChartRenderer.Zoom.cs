using AmiaReforged.Core.Models.Sailing;
using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

public sealed partial class NauticalChartRenderer
{
    private static void ZoomValues(NauticalChartFrame frame, ChartViewport viewport, Dictionary<string, object> values)
    {
        values["chart_background_rect"] = viewport.Background;
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
        {
            var cell = viewport.FogCell(x, y);
            values[$"chart_f{x}_{y}_points"] = new ChartPolygon(cell);
            values[$"chart_f{x}_{y}"] = frame.Discovery[x * 16 + y] != '1' && cell.Width > 0 && cell.Height > 0;
        }
        ZoomRouteValues(frame, frame.Route, viewport, "chart_r", values);
        ZoomRouteValues(frame, frame.Preview, viewport, "chart_p", values);
        var labels = new List<ChartRect>();
        for (int i = 0; i < frame.Features.Length; i++)
        {
            var feature = frame.Features[i]; string key = $"chart_g{i}";
            var rect = viewport.CenteredMarker(feature.X, feature.Y, 12, 12);
            values[key + "_art"] = viewport.Artwork(feature.X, feature.Y, feature.Width, feature.Height);
            values[key + "_rect"] = rect;
            values[key + "_points"] = new ChartPolygon(rect, true);
            bool on = viewport.Contains(feature.X, feature.Y);
            values[key + "_on"] = on;
            if (!feature.Port || frame.ChartMode == SailingChartMode.Tactical) continue;
            var candidates = new[] { new ChartRect(rect.X + 16, rect.Y - 8, 116, 22), new ChartRect(rect.X - 120, rect.Y - 8, 116, 22),
                new ChartRect(rect.X - 20, rect.Y - 26, 116, 22), new ChartRect(rect.X - 20, rect.Y + 16, 116, 22) };
            var label = on ? candidates.FirstOrDefault(r => r.X >= 0 && r.Y >= 0 && r.X + r.Width <= viewport.Size && r.Y + r.Height <= viewport.Size &&
                labels.All(l => !Overlaps(l, r)) && frame.Markers.All(m => !Overlaps(viewport.CenteredMarker(m.X, m.Y, MarkerSize(m.Kind), MarkerSize(m.Kind)), r))) : default;
            values[key + "_label_on"] = label.Width > 0;
            values[key + "_label_rect"] = label;
            if (label.Width > 0) labels.Add(label);
        }
    }

    private static void ZoomRouteValues(NauticalChartFrame frame, ChartRoutePoint[] route, ChartViewport viewport,
        string prefix, Dictionary<string, object> values)
    {
        var own = frame.Markers.FirstOrDefault(m => m.Kind == ChartContactKind.OwnShip);
        var from = own == null ? default : viewport.Project(own.X, own.Y);
        for (int i = 0; i < route.Length; i++)
        {
            var to = viewport.Project(route[i].X, route[i].Y);
            bool on = NauticalChartGeometry.ClipLine(from, to, viewport.Size, out var line) && own != null;
            values[$"{prefix}{i}_a"] = on ? line.From : default(ChartPoint);
            values[$"{prefix}{i}_b"] = on ? line.To : default(ChartPoint);
            values[$"{prefix}{i}_{(prefix == "chart_p" ? "line_on" : "on")}"] = on;
            from = to;
        }
    }

    private static void AddZoomArtwork(NauticalChartFrame frame, List<NuiDrawListItem> drawings)
    {
        for (int i = 0; i < frame.Features.Length; i++)
        {
            var feature = frame.Features[i];
            if (!feature.Port && feature.Sprite.Length > 0)
                drawings.Add(new NuiDrawListImage(feature.Sprite, new NuiBind<NuiRect>($"chart_g{i}_art"))
                    { Aspect = NuiAspect.Fit, HorizontalAlign = NuiHAlign.Center, VerticalAlign = NuiVAlign.Middle });
        }
    }

    private static void AddZoomFeatures(NauticalChartFrame frame, List<NuiDrawListItem> drawings)
    {
        for (int i = 0; i < frame.Features.Length; i++)
        {
            var feature = frame.Features[i]; string key = $"chart_g{i}";
            if (feature.Port) drawings.Add(new ChartBoundPolyLine(Parchment, false, 1.5f, key + "_points") { Enabled = new NuiBind<bool>(key + "_on") });
            else drawings.Add(new NuiDrawListCircle(Parchment, false, 1, new NuiBind<NuiRect>(key + "_rect")) { Enabled = new NuiBind<bool>(key + "_on") });
            if (feature.Port && frame.ChartMode == SailingChartMode.Exploration)
                drawings.Add(new NuiDrawListText(Parchment, new NuiBind<NuiRect>(key + "_label_rect"), feature.Name)
                    { Enabled = new NuiBind<bool>(key + "_label_on") });
        }
    }
}
