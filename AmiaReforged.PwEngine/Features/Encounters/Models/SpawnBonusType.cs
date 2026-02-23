namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Types of bonuses that can be applied as NWN effects to spawned creatures.
/// These form a separate bonus layer that stacks alongside the legacy addon system
/// (Greater/Cagey/Retribution/Ghostly).
/// </summary>
public enum SpawnBonusType
{
    TempHP = 0,
    AC = 1,
    DamageShield = 2,
    Concealment = 3,
    AttackBonus = 4,
    DamageBonus = 5,
    SpellResistance = 6,
    Custom = 99
}
