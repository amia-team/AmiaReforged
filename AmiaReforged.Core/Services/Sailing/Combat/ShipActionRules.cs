using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Pure action rules. No dependency on windows or target-selection ownership.</summary>
public static class ShipActionRules
{
    // Registering a physical NPC deck must never create an unchecked friendly-boarding path.
    public static bool AllowsOrdinaryBoarding(ShipState ship) => ship.ShipType != ShipType.Pirate;

    public static bool IsActive(ShipState? ship) => ship != null && ship.IsActiveWorld &&
        !string.IsNullOrWhiteSpace(ship.AreaResRef) && float.IsFinite(ship.X) &&
        float.IsFinite(ship.Y) && float.IsFinite(ship.Z);

    public static bool CanInteract(ShipState? actor, ShipState? target) =>
        IsActive(actor) && IsActive(target) && !ReferenceEquals(actor, target) &&
        actor!.ShipId != target!.ShipId &&
        string.Equals(actor.AreaResRef, target.AreaResRef, StringComparison.OrdinalIgnoreCase);

    public static bool CanTrade(ShipState source, ShipState merchant, string? portId) =>
        CanInteract(source, merchant) && !source.IsDisabled && !merchant.IsDisabled &&
        merchant.ShipType == ShipType.Merchant && merchant.IsInPort &&
        !string.IsNullOrWhiteSpace(portId) &&
        string.Equals(merchant.CurrentTradePortId, portId, StringComparison.OrdinalIgnoreCase) &&
        Distance(source, merchant) <= ShipEncounterService.EncounterDistance;

    public static float Distance(ShipState actor, ShipState target) =>
        MathF.Sqrt(MathF.Pow(actor.X - target.X, 2) + MathF.Pow(actor.Y - target.Y, 2));

    public static double HeadingDegrees(Heading heading) => ((90 - (int)heading * 45) + 360) % 360;

    private static double FiringAngle(ShipState actor, ShipState target) =>
        (Math.Atan2(target.Y - actor.Y, target.X - actor.X) * 180 / Math.PI -
            HeadingDegrees(actor.Heading) + 360) % 360;

    public static double RelativeBearingDegrees(ShipState actor, ShipState target) =>
        (FiringAngle(actor, target) + 360) % 360;

    // Counterclockwise from the bow in world coordinates. Preserve the original
    // Port/Starboard quarter sectors, including their shared 180-degree boundary.
    public readonly record struct FiringSector(double Start, double End, string Name);
    private static readonly FiringSector[] ForwardSector = [new(315, 405, "BOW")];
    private static readonly FiringSector[] AftSector = [new(135, 225, "STERN")];
    private static readonly FiringSector[] PortSector = [new(90, 180, "PORT")];
    private static readonly FiringSector[] StarboardSector = [new(180, 270, "STARBOARD")];
    private static readonly FiringSector[] BroadsideSectors = [new(45, 135, "PORT"), new(225, 315, "STARBOARD")];

    public static ReadOnlySpan<FiringSector> FiringSectors(WeaponArc arc) => arc switch
    {
        WeaponArc.Forward => ForwardSector, WeaponArc.Aft => AftSector,
        WeaponArc.Port => PortSector, WeaponArc.Starboard => StarboardSector,
        WeaponArc.Broadside => BroadsideSectors, _ => []
    };

    /// <summary>Effective drawable sectors, preserving the legacy signed-remainder rule.
    /// Atan2 returns [-180,180]. For south/southeast headings the old combat angle
    /// can be negative: Forward accepts it, while the other arcs reject it.</summary>
    public static FiringSector[] FiringSectors(WeaponArc arc, Heading heading)
    {
        double degrees = HeadingDegrees(heading);
        double positiveEnd = degrees > 180 ? 540 - degrees : 360;
        if (arc == WeaponArc.Forward) return [new(Math.Min(315, positiveEnd), 405, "BOW")];
        return FiringSectors(arc).ToArray().Where(s => s.Start <= positiveEnd)
            .Select(s => s with { End = Math.Min(s.End, positiveEnd) }).ToArray();
    }

    public static bool InFiringArc(ShipState actor, ShipState target, ShipWeapon weapon)
    {
        double angle = FiringAngle(actor, target);
        const double epsilon = 0.00001;
        // Preserve the existing Forward comparison (angle <= 45), including negative values.
        if (weapon.Arc == WeaponArc.Forward && angle < 0) return true;
        foreach (var sector in FiringSectors(weapon.Arc))
            if ((angle >= sector.Start - epsilon && angle <= sector.End + epsilon) ||
                (sector.End > 360 && angle + 360 >= sector.Start - epsilon && angle + 360 <= sector.End + epsilon)) return true;
        return false;
    }
}
