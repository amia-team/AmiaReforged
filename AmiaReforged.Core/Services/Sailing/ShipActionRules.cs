using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Pure action rules. No dependency on windows or target-selection ownership.</summary>
public static class ShipActionRules
{
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

    public static bool InFiringArc(ShipState actor, ShipState target, ShipWeapon weapon)
    {
        double angle = (Math.Atan2(target.Y - actor.Y, target.X - actor.X) * 180 / Math.PI -
            HeadingDegrees(actor.Heading) + 360) % 360;
        const double epsilon = 0.00001;
        return weapon.Arc switch
        {
            WeaponArc.Forward => angle <= 45 + epsilon || angle >= 315 - epsilon,
            WeaponArc.Aft => angle >= 135 - epsilon && angle <= 225 + epsilon,
            WeaponArc.Port => angle >= 90 - epsilon && angle <= 180 + epsilon,
            WeaponArc.Starboard => angle >= 180 - epsilon && angle <= 270 + epsilon,
            WeaponArc.Broadside => (angle >= 45 - epsilon && angle <= 135 + epsilon) ||
                (angle >= 225 - epsilon && angle <= 315 + epsilon),
            _ => false
        };
    }
}
