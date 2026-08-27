namespace AmiaReforged.Core.Models.Sailing.Ship.Types;

public enum ShipAttackResult
{
    Hit,
    Miss,
    NoTarget,
    AttackerDisabled,
    TargetDisabled,
    OutOfRange,
    OutOfArc,
    NoWeapon,
    NoAmmunition,
    Cooldown,
    NotAuthorized
}
