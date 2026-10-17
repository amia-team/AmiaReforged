using System.Globalization;
using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>The existing 640-world-unit chart transform, independent of display size.</summary>
public static class NauticalChartGeometry
{
    public const float WorldSize = ChartViewport.WorldSize;
    public const string OceanArea = "ocean_01";
    public const float ReferenceDisplaySize = ChartViewport.ReferenceDisplaySize;
    public const int Cells = ChartViewport.Cells;
    public const float WorldCellSize = WorldSize / Cells;
    public const float SidebarWidth = 320;
    public const float SidebarContentWidth = 304;
    public const float MinimumDisplaySize = 416;
    public const float LegendHeight = 56;
    public const float WindowWidthOverhead = 360;
    public const float WindowHeightOverhead = 128;

    public static float InitialSize(float guiWidth, float guiHeight, float guiScale = 100)
    {
        float scale = float.IsFinite(guiScale) && guiScale > 0 ? guiScale / 100 : 1;
        return Quantize(Math.Min(640, Math.Min(guiWidth > 0 ? guiWidth / scale - 384 : 640,
            guiHeight > 0 ? guiHeight / scale - 160 : 640)));
    }
    public static float ResizedSize(float width, float height) => Quantize(Math.Min(width - WindowWidthOverhead, height - WindowHeightOverhead));
    private static float Quantize(float size) => float.IsFinite(size) ? Math.Clamp(MathF.Floor(size / Cells) * Cells, MinimumDisplaySize, 800) : 640;
    public static bool InWorld(float x, float y) => float.IsFinite(x) && float.IsFinite(y) && x >= 0 && y >= 0 && x <= WorldSize && y <= WorldSize;
    public static ChartPoint Project(float x, float y, float size) => ChartViewport.Create(size).Project(x, y);
    /// <summary>Inverse of Project in chart-local NUI pixels; edges are inclusive, outside clicks rejected.</summary>
    public static bool TryUnproject(float x, float y, float size, out ChartPoint world)
    {
        return ChartViewport.Create(size).TryUnproject(x, y, out world);
    }
    public static (int X, int Y) Cell(float x, float y) =>
        (Math.Clamp((int)(x / WorldCellSize), 0, 15), 15 - Math.Clamp((int)(y / WorldCellSize), 0, 15));
    public static ChartRect FogCell(int x, int y, float size) => ChartViewport.Create(size).FogCell(x, y);
    public static ChartRect Artwork(float x, float y, float width, float height, float size)
    {
        // Existing geographic widths/heights were NUI pixels on the original 800-pixel chart.
        // Scale the footprint and preserve its geographic centre; the group scissors excess at an edge.
        return ChartViewport.Create(size).Artwork(x, y, width, height);
    }
    public static ChartRect Marker(float x, float y, float width, float height, float size)
    {
        var point = Project(x, y, size);
        width = Math.Clamp(width, 1, size - 2); height = Math.Clamp(height, 1, size - 2);
        // Conservative in-canvas footprint for caption collision avoidance, not a drawing anchor.
        return new(Math.Clamp(point.X - width / 2, 1, size - width - 1), Math.Clamp(point.Y - height / 2, 1, size - height - 1), width, height);
    }
    public static ChartRect CenteredMarker(float x, float y, float width, float height, float size)
    {
        return ChartViewport.Create(size).CenteredMarker(x, y, width, height);
    }
    public static ChartPoint HeadingVector(Heading heading) => heading switch
    {
        Heading.North => new(0, -1), Heading.NorthEast => new(.7071068f, -.7071068f),
        Heading.East => new(1, 0), Heading.SouthEast => new(.7071068f, .7071068f),
        Heading.South => new(0, 1), Heading.SouthWest => new(-.7071068f, .7071068f),
        Heading.West => new(-1, 0), Heading.NorthWest => new(-.7071068f, -.7071068f), _ => new(1, 0)
    };
    public static string HeadingSuffix(Heading heading) => heading switch
    {
        Heading.North => "n", Heading.NorthEast => "ne", Heading.East => "e", Heading.SouthEast => "se",
        Heading.South => "s", Heading.SouthWest => "sw", Heading.West => "w", Heading.NorthWest => "nw", _ => "e"
    };
    public static string ShipSprite(ShipState ship) => string.IsNullOrWhiteSpace(ship.SpritePrefix) ? "" : $"{ship.SpritePrefix}_{HeadingSuffix(ship.Heading)}";

    // Liang-Barsky clipping; inset protects thin line strokes at the canvas boundary.
    public static bool ClipLine(ChartPoint a, ChartPoint b, float size, out ChartRouteSegment segment)
    {
        segment = new(a, b);
        if (!float.IsFinite(a.X) || !float.IsFinite(a.Y) || !float.IsFinite(b.X) || !float.IsFinite(b.Y)) return false;
        float dx = b.X - a.X, dy = b.Y - a.Y, start = 0, end = 1;
        bool Edge(float p, float q)
        {
            if (p == 0) return q >= 0;
            float r = q / p;
            if (p < 0) { if (r > end) return false; start = Math.Max(start, r); }
            else { if (r < start) return false; end = Math.Min(end, r); }
            return true;
        }
        if (!Edge(-dx, a.X - 1) || !Edge(dx, size - 1 - a.X) || !Edge(-dy, a.Y - 1) || !Edge(dy, size - 1 - a.Y)) return false;
        segment = new(new(a.X + start * dx, a.Y + start * dy), new(a.X + end * dx, a.Y + end * dy));
        return true;
    }
}

public static class NauticalChartReadouts
{
    public static string TargetSummary(ShipState source, ShipState? selected) => selected == null ? "Target: NONE" :
        string.Create(CultureInfo.InvariantCulture, $"{selected.ShipName} | {ShipActionRules.Distance(source, selected):0.0}m");
    public static string[] LogLines(string message, int count)
    {
        var wrapped = new List<string>();
        foreach (string raw in message.Replace("\r", "").Split('\n'))
        {
            string rest = raw;
            while (rest.Length > 44)
            {
                int split = rest.LastIndexOf(' ', 44);
                if (split <= 0) split = 44;
                wrapped.Add(rest[..split]); rest = rest[split..].TrimStart();
            }
            wrapped.Add(rest);
        }
        var result = new string[count];
        Array.Fill(result, "");
        string[] tail = wrapped.TakeLast(count).ToArray();
        Array.Copy(tail, result, tail.Length);
        return result;
    }
}

public static class NauticalChartProjection
{
    public static ChartContactKind Classify(ShipType type) => type switch
    { ShipType.Player => ChartContactKind.Player, ShipType.Merchant => ChartContactKind.Merchant, ShipType.Pirate => ChartContactKind.Pirate, _ => ChartContactKind.Neutral };

    public static NauticalChartFrame Capture(ShipState ship, ShipNavigationService navigation,
        IEnumerable<VisibleShipContact> visibleShips, IEnumerable<OceanContact> visibleOcean,
        IEnumerable<ChartLandmark> landmarks, IEnumerable<ChartMarker> markers, IEnumerable<IslandLocation> ports,
        Func<int, int, bool> discovered, string? selectedSailingTarget = null)
    {
        var fog = new char[256];
        for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++) fog[x * 16 + y] = discovered(x, y) ? '1' : '0';
        bool Known(float x, float y)
        { var cell = NauticalChartGeometry.Cell(x, y); return NauticalChartGeometry.InWorld(x, y) && fog[cell.X * 16 + cell.Y] == '1'; }
        bool SameArea(string area) => string.Equals(area, ship.AreaResRef, StringComparison.OrdinalIgnoreCase);
        var contacts = new List<ChartShipMarker>();
        var identities = new HashSet<string>(StringComparer.Ordinal) { ship.ShipId };
        foreach (var contact in visibleShips.OrderBy(c => c.Ship.ShipId, StringComparer.Ordinal))
        {
            var other = contact.Ship;
            if (!ShipActionRules.CanInteract(ship, other) || ShipActionRules.Distance(ship, other) > 70 || !Known(other.X, other.Y) || !identities.Add(other.ShipId)) continue;
            contacts.Add(new(other.ShipId, other.ShipName, other.X, other.Y, other.Heading, NauticalChartGeometry.ShipSprite(other), Classify(other.ShipType), other.ShipId == selectedSailingTarget));
        }
        foreach (var contact in visibleOcean.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (!ShipActionRules.IsActive(ship) || !contact.Visible || contact.ConvertedToShip || !SameArea(contact.AreaResRef) ||
                MathF.Sqrt(MathF.Pow(contact.X - ship.X, 2) + MathF.Pow(contact.Y - ship.Y, 2)) > OceanContactService.RevealRange ||
                !Known(contact.X, contact.Y) || !identities.Add(contact.Id)) continue;
            contacts.Add(new(contact.Id, contact.Name, contact.X, contact.Y, null, "", contact.Type switch
                { EncounterType.Merchant => ChartContactKind.Merchant, EncounterType.Pirate => ChartContactKind.Pirate, _ => ChartContactKind.Neutral }));
        }
        contacts = contacts.OrderBy(c => c.Kind).ThenBy(c => c.Id, StringComparer.Ordinal).ToList();
        if (ShipActionRules.IsActive(ship) && NauticalChartGeometry.InWorld(ship.X, ship.Y))
            contacts.Add(new(ship.ShipId, ship.ShipName, ship.X, ship.Y, ship.Heading, NauticalChartGeometry.ShipSprite(ship), ChartContactKind.OwnShip));
        var features = new List<ChartFeature>();
        foreach (var landmark in landmarks.Where(l => SameArea(l.AreaResRef) && Known(l.X, l.Y)))
            features.Add(new($"land:{landmark.Sprite}:{landmark.X}:{landmark.Y}", "Landmark", landmark.X, landmark.Y, landmark.Sprite, landmark.Width, landmark.Height, false));
        foreach (var marker in markers.Where(m => SameArea(m.AreaResRef) && Known(m.X, m.Y)))
            features.Add(new($"mark:{marker.Sprite}:{marker.X}:{marker.Y}", "Chart marker", marker.X, marker.Y, marker.Sprite, marker.Size, marker.Size, false));
        foreach (var port in ports.Where(p => SameArea(p.OceanArea) && Known(p.OceanX, p.OceanY)))
            features.Add(new($"port:{port.Id}", port.Name, port.OceanX, port.OceanY, "", 12, 12, true));
        var activeRoute = navigation.IsNavigating(ship);
        var route = navigation.GetRoute(ship);
        if (route?.IsComplete == true) activeRoute = false;
        var localRoute = new List<ChartRoutePoint>();
        var remaining = activeRoute && route != null ? route.Waypoints.Skip(Math.Max(0, route.CurrentWaypointIndex)).ToArray() : [];
        if (activeRoute)
        {
            // This is a projection of the actual remaining route, never a new route calculation.
            if (route != null)
            {
                for (int i = 0; i < remaining.Length; i++)
                {
                    var waypoint = remaining[i];
                    if (!SameArea(waypoint.AreaResRef)) break;
                    if (!float.IsFinite(waypoint.X) || !float.IsFinite(waypoint.Y)) break;
                    localRoute.Add(new(waypoint.AreaResRef, waypoint.X, waypoint.Y, !route.Loop && i == remaining.Length - 1));
                }
            }
            else if (SameArea(ship.DestinationAreaResRef!)) localRoute.Add(new(ship.DestinationAreaResRef!, ship.DestinationX, ship.DestinationY, true));
        }
        static string Position(string area, float x, float y) => string.Create(CultureInfo.InvariantCulture, $"{area} ({x:0.#}, {y:0.#})");
        string destination = "Destination: --";
        if (activeRoute)
        {
            var next = route?.CurrentWaypoint;
            destination = "Next: " + Position(next?.AreaResRef ?? ship.DestinationAreaResRef!, next?.X ?? ship.DestinationX, next?.Y ?? ship.DestinationY);
            if (remaining.Length > 0)
            { var final = remaining[^1]; destination += "\n" + (route!.Loop ? "Circuit end: " : "Destination: ") + Position(final.AreaResRef, final.X, final.Y); }
            if (localRoute.Count == 0) destination += "\nNext leg is on another ocean chart.";
        }
        return new(ship.ShipId, ship.AreaResRef, new(fog), contacts.ToArray(), features.DistinctBy(f => f.Id).ToArray(), localRoute.ToArray(),
            "Heading: " + NauticalChartGeometry.HeadingSuffix(ship.Heading).ToUpperInvariant(),
            "Movement: " + (ship.IsDisabled ? "DISABLED" : ship.Underway ? "UNDERWAY" : "STOPPED"),
            "Mode: " + (activeRoute ? route?.IsChartCourse == true ? "AUTOPILOT" : route?.Loop == true ? "ROUTE / CIRCUIT" : "ROUTE" : ship.Underway ? "MANUAL" : "STOPPED"),
            destination, "Waypoints remaining: " + (activeRoute ? route != null ? remaining.Length.ToString(CultureInfo.InvariantCulture) : "1" : "0"))
            { ChartCourse = activeRoute && route?.IsChartCourse == true };
    }

    public static ChartRouteSegment[] Segments(NauticalChartFrame frame, float size)
    {
        var viewport = frame.Viewport ?? ChartViewport.Create(size);
        var own = frame.Markers.LastOrDefault(m => m.Kind == ChartContactKind.OwnShip);
        if (own == null) return [];
        var result = new List<ChartRouteSegment>(); var from = viewport.Project(own.X, own.Y);
        foreach (var waypoint in frame.Route)
        {
            var to = viewport.Project(waypoint.X, waypoint.Y);
            if (NauticalChartGeometry.ClipLine(from, to, size, out var segment)) result.Add(segment);
            from = to;
        }
        return result.ToArray();
    }
}
