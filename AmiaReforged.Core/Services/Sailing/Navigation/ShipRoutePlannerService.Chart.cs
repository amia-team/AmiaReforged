using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public partial class ShipRoutePlannerService
{
    /// <summary>
    /// Bounded same-area rectilinear search. Axis-aligned legs match the existing eight-heading
    /// steering exactly, unlike a straight visibility edge that the ship would approximate.
    /// Two units of clearance allow for the existing one-unit waypoint arrival radius.
    /// </summary>
    public ShipNavigationRoute? BuildChartRoute(ShipState ship, ChartPoint destination, ShipObstacleService obstacles)
    {
        if (!string.Equals(ship.AreaResRef, NauticalChartGeometry.OceanArea, StringComparison.OrdinalIgnoreCase) ||
            !_sailingAreaService.TryGetArea(ship.AreaResRef, out var area) || area == null ||
            !NauticalChartGeometry.InWorld(ship.X, ship.Y) || !NauticalChartGeometry.InWorld(destination.X, destination.Y) ||
            !float.IsFinite(ship.Z) || !obstacles.CanMoveTo(ship.AreaResRef, ship.X, ship.Y) ||
            !obstacles.CanMoveTo(ship.AreaResRef, destination.X, destination.Y)) return null;

        var xs = new SortedSet<float> { ship.X, destination.X, area.MinX, area.MaxX };
        var ys = new SortedSet<float> { ship.Y, destination.Y, area.MinY, area.MaxY };
        foreach (var obstacle in obstacles.GetObstacles(ship.AreaResRef))
        {
            if (obstacle.MinX - 2 >= area.MinX) xs.Add(obstacle.MinX - 2);
            if (obstacle.MaxX + 2 <= area.MaxX) xs.Add(obstacle.MaxX + 2);
            if (obstacle.MinY - 2 >= area.MinY) ys.Add(obstacle.MinY - 2);
            if (obstacle.MaxY + 2 <= area.MaxY) ys.Add(obstacle.MaxY + 2);
        }
        float[] x = xs.ToArray(), y = ys.ToArray();
        if ((long)x.Length * y.Length > 16384) return null; // Configuration cannot cause an unbounded search.
        int Index(float px, float py) => Array.IndexOf(x, px) * y.Length + Array.IndexOf(y, py);
        var start = Index(ship.X, ship.Y); var goal = Index(destination.X, destination.Y);
        var costs = new Dictionary<int, float> { [start] = 0 };
        var previous = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, float>(); queue.Enqueue(start, 0);
        while (queue.TryDequeue(out int current, out float cost))
        {
            if (cost > costs[current]) continue;
            if (current == goal) break;
            int cx = current / y.Length, cy = current % y.Length;
            foreach (var (nx, ny) in new[] { (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1) })
            {
                if (nx < 0 || ny < 0 || nx >= x.Length || ny >= y.Length ||
                    !obstacles.IsSegmentClear(ship.AreaResRef, x[cx], y[cy], x[nx], y[ny])) continue;
                int next = nx * y.Length + ny;
                float candidate = cost + MathF.Abs(x[nx] - x[cx]) + MathF.Abs(y[ny] - y[cy]);
                if (costs.TryGetValue(next, out float best) && candidate >= best) continue;
                costs[next] = candidate; previous[next] = current; queue.Enqueue(next, candidate);
            }
        }
        if (!costs.ContainsKey(goal)) return null;
        var points = new List<ChartPoint>();
        for (int node = goal; node != start; node = previous[node]) points.Add(new(x[node / y.Length], y[node % y.Length]));
        points.Reverse();
        if (points.Count == 0) points.Add(destination);
        // Remove only collinear graph nodes, retaining every actual turn.
        for (int i = points.Count - 2; i >= 0; i--)
        {
            ChartPoint before = i == 0 ? new(ship.X, ship.Y) : points[i - 1];
            if (before.X == points[i].X && points[i].X == points[i + 1].X ||
                before.Y == points[i].Y && points[i].Y == points[i + 1].Y) points.RemoveAt(i);
        }
        var route = new ShipNavigationRoute { ShipName = ship.ShipName, IsChartCourse = true };
        foreach (var point in points)
            route.Waypoints.Add(new() { AreaResRef = ship.AreaResRef, X = point.X, Y = point.Y, Z = ship.Z,
                Description = "Chart course" });
        return route;
    }
}
