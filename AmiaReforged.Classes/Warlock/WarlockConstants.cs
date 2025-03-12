using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using NLog;
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
    public static int RangedTouch(uint nwnObjectId, uint targetObject)
    {
        NwObject? caster = nwnObjectId.ToNwObject();
        NwObject? target = targetObject.ToNwObject();
        if (target is not NwCreature targetCreature) return 1;
        if (caster is not NwCreature casterCreature) return 1;

        TouchAttackResult result = casterCreature.TouchAttackRanged(targetCreature, true);

        return result == TouchAttackResult.Hit ? TRUE : FALSE;
    }

    /// <summary>
    ///     A nice looking string color for warlock
    /// </summary>
    public static string String(string message) => NwEffects.ColorString(message, rgb: "517");
}