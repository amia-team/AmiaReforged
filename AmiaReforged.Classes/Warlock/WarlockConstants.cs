using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock;

// Constants are defined here for ease of use when calling in service handlers.

public static class WarlockConstants
{
    /// <summary>
    ///     Warlock DC: Base 10 + Warlock levels / 3 + Charisma modidifer
    /// </summary>
    public static int CalculateDc(uint caster) =>
        10 + GetLevelByClass(57, caster) / 3 + GetAbilityModifier(ABILITY_CHARISMA, caster);

    /// <summary>
    ///     A ranged touch attack for warlock that takes crit conditions into account.
    /// </summary>
    public static int RangedTouch(uint targetObject)
    {
        int touchAttackRanged = TouchAttackRanged(targetObject);
        if (touchAttackRanged == 0) return 0;
        if (GetIsImmune(targetObject, IMMUNITY_TYPE_CRITICAL_HIT) == TRUE && touchAttackRanged == 2) return 1;
        if (GetRacialType(targetObject) == RACIAL_TYPE_CONSTRUCT || GetRacialType(targetObject) == RACIAL_TYPE_UNDEAD ||
            GetRacialType(targetObject) == RACIAL_TYPE_ELEMENTAL && touchAttackRanged == 2) return 1;
        if (touchAttackRanged == 1) return 1;
        return 2;
    }

    /// <summary>
    ///     A nice looking string color for warlock
    /// </summary>
    public static string String(string message) => NwEffects.ColorString(message, rgb: "517");
}