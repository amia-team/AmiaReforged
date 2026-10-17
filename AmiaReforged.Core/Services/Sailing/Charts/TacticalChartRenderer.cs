using AmiaReforged.Core.Models.Sailing;
using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Fixed drawing slots with dynamic binds: even clipped/disabled sectors do not rebuild artwork.</summary>
public static class TacticalChartRenderer
{
    public static Color ColorFor(ChartContactKind kind) => kind switch
    {
        ChartContactKind.OwnShip => new Color(218, 187, 113),
        ChartContactKind.Pirate => new Color(193, 100, 91),
        ChartContactKind.Merchant => new Color(107, 177, 159),
        ChartContactKind.Player => new Color(127, 178, 201),
        _ => new Color(177, 181, 180)
    };

    public static void Values(NauticalChartFrame frame, float size, Dictionary<string, object> values, TacticalMarker[]? placed = null)
    {
        var markers = placed ?? TacticalChartProjection.PlaceMarkers(frame, size);
        var lines = TacticalChartProjection.WeaponLines(frame, size);
        for (int i = 0; i < lines.Length; i++) LineValues(values, $"tac_o{i}", lines[i]);
        bool available = frame.Tactical?.WeaponsAvailable == true;
        values["tac_range_color"] = available ? ChartOverlayDrawing.Available : ChartOverlayDrawing.Unavailable;
        for (int i = 0; i < 2; i++)
        {
            bool target = TacticalChartProjection.SectorTargetsSelected(frame, i);
            var color = !available ? ChartOverlayDrawing.Unavailable : target ? ChartOverlayDrawing.Target : ChartOverlayDrawing.Available;
            values[$"tac_sector{i}_color"] = color;
            values[$"tac_sector{i}_fill"] = !available ? new Color(89, 117, 147, 14) :
                target ? new Color(255, 104, 48, 34) : new Color(93, 228, 255, 22);
            var points = TacticalChartProjection.SectorPoints(frame, size, i);
            values[$"tac_sector{i}_points"] = points;
            values[$"tac_sector{i}_fill_visible"] = points.Count > 0;
        }
        for (int i = 0; i < markers.Length; i++)
        {
            var marker = markers[i]; var c = marker.Center;
            string key = $"tac_m{i}";
            values[key + "_anchor"] = new ChartRect(marker.Anchor.X - 1, marker.Anchor.Y - 1, 2, 2);
            values[key + "_ring"] = new ChartRect(c.X - 7, c.Y - 7, 14, 14);
            values[key + "_body"] = new ChartRect(c.X - 3, c.Y - 3, 6, 6);
            values[key + "_selected"] = marker.Ship.Selected;
            LineValues(values, key + "_leader", TacticalChartProjection.DistanceSquared(marker.Anchor, c) > .1f
                ? TacticalChartProjection.Clip(marker.Anchor, c, size) : new(default, default, false));
            float r = marker.Ship.Kind == ChartContactKind.OwnShip ? 4 : 3;
            ChartPoint[] vertices = marker.Ship.Kind switch
            {
                ChartContactKind.Player => [new(c.X - r, c.Y - r), new(c.X + r, c.Y - r), new(c.X + r, c.Y + r), new(c.X - r, c.Y + r)],
                ChartContactKind.Pirate => [new(c.X, c.Y - r), new(c.X + r, c.Y + r), new(c.X - r, c.Y + r)],
                _ => [new(c.X, c.Y - r), new(c.X + r, c.Y), new(c.X, c.Y + r), new(c.X - r, c.Y)]
            };
            for (int j = 0; j < 4; j++)
                LineValues(values, key + $"_hull{j}", j < vertices.Length && marker.Ship.Kind != ChartContactKind.Neutral
                    ? TacticalChartProjection.Clip(vertices[j], vertices[(j + 1) % vertices.Length], size) : new(default, default, false));
            var direction = TacticalChartProjection.HeadingVector(marker.Ship.Heading ?? Heading.North);
            ChartPoint tip = new(c.X + direction.X * 8, c.Y + direction.Y * 8);
            ChartPoint left = new(tip.X - direction.X * 3 - direction.Y * 2, tip.Y - direction.Y * 3 + direction.X * 2);
            ChartPoint right = new(tip.X - direction.X * 3 + direction.Y * 2, tip.Y - direction.Y * 3 - direction.X * 2);
            ChartPoint[] ends = [c, left, right];
            for (int j = 0; j < 3; j++)
                LineValues(values, key + $"_heading{j}", marker.Ship.Heading != null
                    ? TacticalChartProjection.Clip(ends[j], tip, size) : new(default, default, false));
        }
        int crowded = markers.Count(m => m.Crowded);
        values["chart_legend1"] = TacticalChartProjection.TargetReadout(frame.Tactical);
        values["chart_legend2"] = TacticalChartProjection.WeaponReadout(frame.Tactical);
        values["chart_legend_help"] = TacticalChartProjection.Details(frame.Tactical, crowded);
        values["chart_contacts"] = $"Visible: {markers.Count(m => m.Ship.Kind != ChartContactKind.OwnShip)} | Crowded: {crowded}";
    }

    private static void LineValues(Dictionary<string, object> values, string key, TacticalLine line)
    {
        values[key + "_a"] = line.From; values[key + "_b"] = line.To; values[key + "_on"] = line.Visible;
    }

    private static NuiDrawListLine Line(string key, Color color, float width = 1) => new(color, false, width,
        new NuiBind<NuiVector>(key + "_a"), new NuiBind<NuiVector>(key + "_b")) { Enabled = new NuiBind<bool>(key + "_on") };

    public static void AddInstruments(List<NuiDrawListItem> drawings)
    {
        for (int i = 0; i < TacticalChartProjection.OverlayLines; i++)
        {
            if (i == TacticalChartProjection.OverlayLines - 1)
                ChartOverlayDrawing.AddBackedLine(drawings, $"tac_o{i}", new Color(225, 207, 153, 210), 1.25f);
            else
            {
                string key = i < TacticalChartProjection.RangeSegments ? "tac_range_color" :
                    $"tac_sector{(i - TacticalChartProjection.RangeSegments) / (TacticalChartProjection.SectorSegments + 2)}_color";
                ChartOverlayDrawing.AddBackedLine(drawings, $"tac_o{i}", new NuiBind<Color>(key),
                    i < TacticalChartProjection.RangeSegments ? 1.1f : 2.2f);
            }
        }
    }

    public static void AddSectorFills(List<NuiDrawListItem> drawings)
    {
        for (int i = 0; i < 2; i++)
            drawings.Add(new ChartBoundPolyLine(ChartOverlayDrawing.Available, true, 0, $"tac_sector{i}_points")
            { Color = new NuiBind<Color>($"tac_sector{i}_fill"), Enabled = new NuiBind<bool>($"tac_sector{i}_fill_visible") });
    }

    public static void AddMarkers(TacticalMarker[] markers, List<NuiDrawListItem> drawings)
    {
        // Leaders first, so another ship's connector never covers a foreground symbol.
        for (int i = 0; i < markers.Length; i++) drawings.Add(Line($"tac_m{i}_leader", new Color(175, 173, 154, 140)));
        for (int i = 0; i < markers.Length; i++)
        {
            string key = $"tac_m{i}"; var marker = markers[i].Ship; var color = ColorFor(marker.Kind);
            drawings.Add(new NuiDrawListCircle(color, true, 0, new NuiBind<NuiRect>(key + "_anchor")));
            if (marker.Kind == ChartContactKind.Neutral)
                drawings.Add(new NuiDrawListCircle(color, false, 1, new NuiBind<NuiRect>(key + "_body")));
            for (int j = 0; j < 4; j++) drawings.Add(Line(key + $"_hull{j}", color, marker.Kind == ChartContactKind.OwnShip ? 1.5f : 1));
            for (int j = 0; j < 3; j++) drawings.Add(Line(key + $"_heading{j}", color));
            drawings.Add(new NuiDrawListCircle(new Color(241, 219, 165), false, 1.5f,
                new NuiBind<NuiRect>(key + "_ring")) { Enabled = new NuiBind<bool>(key + "_selected") });
        }
    }
}
