using AmiaReforged.Core.Models.Sailing;
using Anvil.API;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Small drawing primitives shared by course and combat overlays; owns no lifecycle or gameplay state.</summary>
public static class ChartOverlayDrawing
{
    public static readonly Color Backing = new(6, 16, 25, 235);
    public static readonly Color Course = new(244, 58, 65, 255);
    public static readonly Color Available = new(93, 228, 255, 245);
    public static readonly Color Target = new(255, 104, 48, 255);
    public static readonly Color Unavailable = new(89, 117, 147, 210);

    public static void AddBackedLine(List<NuiDrawListItem> drawings, string key, NuiProperty<Color> color, float width)
    {
        var a = new NuiBind<NuiVector>(key + "_a"); var b = new NuiBind<NuiVector>(key + "_b");
        var enabled = new NuiBind<bool>(key + "_on");
        drawings.Add(new NuiDrawListLine(Backing, false, width + 2.5f, a, b) { Enabled = enabled });
        drawings.Add(new NuiDrawListLine(color, false, width, a, b) { Enabled = enabled });
    }

    /// <summary>Clip a convex sector polygon to the same inset as chart instrument lines.</summary>
    public static List<float> ClipPolygon(IEnumerable<ChartPoint> vertices, float size)
    {
        var points = vertices.ToList();
        if (!float.IsFinite(size) || size < 2 || points.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y))) return [];
        for (int edge = 0; edge < 4 && points.Count > 0; edge++)
        {
            bool vertical = edge < 2, minimum = edge % 2 == 0;
            float boundary = minimum ? 1 : size - 1;
            float Coordinate(ChartPoint p) => vertical ? p.X : p.Y;
            bool Inside(ChartPoint p) => minimum ? Coordinate(p) >= boundary : Coordinate(p) <= boundary;
            var clipped = new List<ChartPoint>(); var previous = points[^1];
            foreach (var current in points)
            {
                bool before = Inside(previous), after = Inside(current);
                if (before != after)
                {
                    float t = (boundary - Coordinate(previous)) / (Coordinate(current) - Coordinate(previous));
                    clipped.Add(new(previous.X + (current.X - previous.X) * t, previous.Y + (current.Y - previous.Y) * t));
                }
                if (after) clipped.Add(current);
                previous = current;
            }
            points = clipped;
        }
        if (points.Count < 3) return [];
        double area = 0;
        for (int i = 0; i < points.Count; i++)
        { var a = points[i]; var b = points[(i + 1) % points.Count]; area += (double)a.X * b.Y - (double)b.X * a.Y; }
        if (Math.Abs(area) < .001) return [];
        var result = new List<float>((points.Count + 1) * 2);
        foreach (var point in points.Append(points[0])) { result.Add(point.X); result.Add(point.Y); }
        return result;
    }
}
