using System.Globalization;
using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public sealed record TacticalMarker(ChartShipMarker Ship, ChartPoint Anchor, ChartPoint Center, bool Crowded);
public sealed record TacticalLine(ChartPoint From, ChartPoint To, bool Visible = true);

/// <summary>Chart-local geometry only. Displacement never enters navigation, targeting or combat.</summary>
public static class TacticalChartProjection
{
    public const float Separation = 18;
    public const float MaximumDisplacement = 36;
    public const int RangeSegments = 32;
    public const int SectorSegments = 12;
    public const int OverlayLines = RangeSegments + 2 * (SectorSegments + 2) + 1;
    private static readonly TacticalLine Hidden = new(default, default, false);

    public static TacticalChartStatus Capture(NauticalChartFrame frame, ShipState source, ShipState? selected,
        ShipCombatService combat, bool authorized)
    {
        // The authoritative chart projection already enforces visibility, fog and area.
        // Do not turn an unplottable Sailing selection into a bearing or status disclosure.
        var target = selected != null && frame.Markers.Any(m => m.Id == selected.ShipId && m.Selected) ? selected : null;
        var weapon = combat.GetWeapon(source);
        float? distance = target == null ? null : ShipActionRules.Distance(source, target);
        bool armed = weapon.Damage > 0 && weapon.MaxRange > 0;
        var inspection = combat.InspectAttack(source, target, authorized);
        return new(weapon, target?.ShipId, target?.ShipName ?? "NONE", distance,
            target == null ? null : ShipActionRules.RelativeBearingDegrees(source, target),
            armed && distance <= weapon.MaxRange, armed && target != null && ShipActionRules.InFiringArc(source, target, weapon),
            AttackText(inspection))
        {
            // Validation precedes cooldown in InspectAttack. A missing or invalid target
            // must not make reloading weapons appear available.
            WeaponsAvailable = armed && authorized && ShipActionRules.IsActive(source) && !source.IsDisabled &&
                inspection is not { InvalidAttacker: true } && combat.GetCooldownRemaining(source) == TimeSpan.Zero
        };
    }

    private static string AttackText(ShipCombatService.ShipAttackResult? result) => result switch
    {
        null => "READY",
        { HasNoWeapons: true } => "UNARMED",
        { IsNotAuthorized: true } => "NOT AUTHORIZED",
        { InvalidAttacker: true } => "INACTIVE",
        { AttackerDisabled: true } => "HULL DISABLED",
        { NoTargetFound: true } => "NO TARGET",
        { TargetDisabled: true } => "TARGET DISABLED",
        { IsOutOfRange: true } => "OUT OF RANGE",
        { IsOutOfArc: true } => "OUT OF ARC",
        { IsCooldown: true } => "RELOADING",
        _ => "UNAVAILABLE"
    };

    public static string Bearing(double? degrees, float? distance)
    {
        if (degrees == null) return "--";
        if (distance == 0) return "COINCIDENT";
        double angle = degrees.Value;
        if (angle < .00001 || angle > 359.99999) return "BOW 0 deg";
        if (Math.Abs(angle - 180) < .00001) return "STERN 180 deg";
        return string.Create(CultureInfo.InvariantCulture, $"{(angle < 180 ? "PORT" : "STBD")} {Math.Min(angle, 360 - angle):0} deg");
    }

    public static string TargetReadout(TacticalChartStatus? status)
    {
        if (status?.TargetId == null) return "TARGET: NONE | DIST: -- | BEARING: --";
        string name = status.TargetName.Length > 16 ? status.TargetName[..15] + "~" : status.TargetName;
        return string.Create(CultureInfo.InvariantCulture, $"TARGET: {name} | DIST: {status.Distance:0.0} | {Bearing(status.RelativeBearing, status.Distance)}");
    }

    public static string WeaponReadout(TacticalChartStatus? status) => status == null ? "ATTACK: UNAVAILABLE" :
        status.Weapon.Damage <= 0 ? "WEAPON: UNARMED | ARC: -- | ATTACK: UNARMED" :
        $"RANGE: {(status.TargetId == null ? "--" : status.InRange ? "IN" : "OUT")} | ARC: {(status.TargetId == null ? "--" : status.InArc ? "VALID" : "INVALID")} | ATTACK: {status.AttackStatus}";

    public static string Details(TacticalChartStatus? status, int crowded) => status == null ? "" :
        string.Create(CultureInfo.InvariantCulture, $"Target: {status.TargetName}. {status.Weapon.DisplayName}; max range {status.Weapon.MaxRange:0.0}; sectors: ") +
        (status.Weapon.Damage <= 0 ? "NONE" : string.Join(" / ", ShipActionRules.FiringSectors(status.Weapon.Arc).ToArray().Select(s => s.Name))) +
        ". Ice blue: available; orange-red: selected sector can attack now; slate: weapons unavailable. Red dashes: actual course" +
        ". Dots are true positions; thin leaders connect displaced symbols. Gold: yours; red triangle: pirate; teal diamond: merchant; blue square: player; grey circle: neutral. " +
        (crowded > 0 ? $"{crowded} crowded symbols: separation limited to 36 pixels. Use the Sailing target list to identify them." : "");

    public static TacticalMarker[] PlaceMarkers(NauticalChartFrame frame, float size)
    {
        var viewport = frame.Viewport ?? ChartViewport.Create(size);
        var placed = new List<TacticalMarker>();
        // Reserve own ship, then selection. Identity order makes ties independent of enumeration order.
        foreach (var marker in frame.Markers.Where(m => viewport.Contains(m.X, m.Y)).OrderBy(m => m.Kind == ChartContactKind.OwnShip ? 0 : m.Selected ? 1 : 2)
                     .ThenBy(m => m.Id, StringComparer.Ordinal))
        {
            ChartPoint anchor = viewport.Project(marker.X, marker.Y);
            ChartPoint center = anchor;
            float best = float.NegativeInfinity;
            if (marker.Kind != ChartContactKind.OwnShip)
            {
                foreach (var candidate in Candidates(anchor, size))
                {
                    float clearance = placed.Count == 0 ? float.PositiveInfinity : placed.Min(p => DistanceSquared(candidate, p.Center));
                    if (clearance > best) { best = clearance; center = candidate; }
                    if (clearance >= Separation * Separation) break;
                }
            }
            placed.Add(new(marker, anchor, center, marker.Kind != ChartContactKind.OwnShip && best < Separation * Separation));
        }
        // Selected contact above other contacts; own ship above everything.
        return placed.OrderBy(p => p.Ship.Kind == ChartContactKind.OwnShip ? 2 : p.Ship.Selected ? 1 : 0)
            .ThenBy(p => p.Ship.Kind).ThenBy(p => p.Ship.Id, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<ChartPoint> Candidates(ChartPoint anchor, float size)
    {
        ChartPoint Clamp(ChartPoint p) => new(Math.Clamp(p.X, 9, size - 9), Math.Clamp(p.Y, 9, size - 9));
        yield return Clamp(anchor);
        for (int radius = 6; radius <= MaximumDisplacement; radius += 6)
        for (int step = 0; step < 24; step++)
        {
            double angle = step * Math.PI / 12;
            var point = Clamp(new(anchor.X + radius * (float)Math.Cos(angle), anchor.Y + radius * (float)Math.Sin(angle)));
            if (DistanceSquared(anchor, point) <= MaximumDisplacement * MaximumDisplacement + .001f) yield return point;
        }
    }

    public static float DistanceSquared(ChartPoint a, ChartPoint b) => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

    public static ChartPoint HeadingVector(Heading heading)
    {
        double radians = ShipActionRules.HeadingDegrees(heading) * Math.PI / 180;
        return new((float)Math.Cos(radians), -(float)Math.Sin(radians));
    }

    public static TacticalLine Clip(ChartPoint a, ChartPoint b, float size) =>
        NauticalChartGeometry.ClipLine(a, b, size, out var line) ? new(line.From, line.To) : Hidden;

    public static bool SectorTargetsSelected(NauticalChartFrame frame, int index)
    {
        if (frame.Tactical is not { WeaponsAvailable: true, InRange: true, InArc: true, AttackStatus: "READY", RelativeBearing: { } bearing } status ||
            !frame.Markers.Any(m => m.Selected && m.Id == status.TargetId) ||
            frame.Markers.FirstOrDefault(m => m.Kind == ChartContactKind.OwnShip)?.Heading is not { } heading) return false;
        var sectors = ShipActionRules.FiringSectors(status.Weapon.Arc, heading);
        if (index < 0 || index >= sectors.Length) return false;
        var sector = sectors[index]; const double epsilon = .00001;
        bool Contains(double angle) => angle >= sector.Start - epsilon && angle <= sector.End + epsilon;
        return Contains(bearing) || Contains(bearing + 360);
    }

    public static List<float> SectorPoints(NauticalChartFrame frame, float size, int index)
    {
        var own = frame.Markers.FirstOrDefault(m => m.Kind == ChartContactKind.OwnShip);
        if (own?.Heading is not { } heading || frame.Tactical is not { Weapon.Damage: > 0, Weapon.MaxRange: > 0 } status) return [];
        var sectors = ShipActionRules.FiringSectors(status.Weapon.Arc, heading);
        if (index < 0 || index >= sectors.Length) return [];
        var viewport = frame.Viewport ?? ChartViewport.Create(size); var sector = sectors[index];
        var points = new List<ChartPoint> { viewport.Project(own.X, own.Y) };
        for (int i = 0; i <= SectorSegments; i++)
        {
            double degrees = ShipActionRules.HeadingDegrees(heading) + sector.Start + (sector.End - sector.Start) * i / SectorSegments;
            double radians = degrees * Math.PI / 180;
            points.Add(viewport.Project(own.X + status.Weapon.MaxRange * (float)Math.Cos(radians),
                own.Y + status.Weapon.MaxRange * (float)Math.Sin(radians)));
        }
        return ChartOverlayDrawing.ClipPolygon(points, size);
    }

    public static TacticalLine[] WeaponLines(NauticalChartFrame frame, float size)
    {
        var viewport = frame.Viewport ?? ChartViewport.Create(size);
        var lines = Enumerable.Repeat(Hidden, OverlayLines).ToArray();
        var own = frame.Markers.FirstOrDefault(m => m.Kind == ChartContactKind.OwnShip);
        if (own?.Heading == null) return lines;
        var origin = viewport.Project(own.X, own.Y);
        var status = frame.Tactical;
        if (status is { Weapon.Damage: > 0, Weapon.MaxRange: > 0 })
        {
            // All radii are world units projected by the same transform as fog, ships and routes.
            ChartPoint At(double degrees)
            {
                double radians = degrees * Math.PI / 180;
                return viewport.Project(own.X + status.Weapon.MaxRange * (float)Math.Cos(radians),
                    own.Y + status.Weapon.MaxRange * (float)Math.Sin(radians));
            }
            for (int i = 0; i < RangeSegments; i++)
                if (i % 2 == 0) lines[i] = Clip(At(i * 360.0 / RangeSegments), At((i + 1) * 360.0 / RangeSegments), size);
            int slot = RangeSegments;
            foreach (var sector in ShipActionRules.FiringSectors(status.Weapon.Arc, own.Heading.Value))
            {
                double start = ShipActionRules.HeadingDegrees(own.Heading.Value) + sector.Start;
                double end = ShipActionRules.HeadingDegrees(own.Heading.Value) + sector.End;
                lines[slot++] = Clip(origin, At(start), size);
                lines[slot++] = Clip(origin, At(end), size);
                for (int i = 0; i < SectorSegments; i++)
                    lines[slot++] = Clip(At(start + (end - start) * i / SectorSegments), At(start + (end - start) * (i + 1) / SectorSegments), size);
            }
        }
        var target = frame.Markers.FirstOrDefault(m => m.Selected && m.Id == status?.TargetId);
        if (target != null) lines[^1] = Clip(origin, viewport.Project(target.X, target.Y), size);
        return lines;
    }
}
