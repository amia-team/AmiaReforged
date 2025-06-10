using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
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
        const int miss = 0;
        const int hit = 1;
        const int criticalHit = 2;
        
        NwObject? caster = nwnObjectId.ToNwObject();
        NwObject? target = targetObject.ToNwObject();
        if (target is not NwCreature targetCreature) return 1;
        if (caster is not NwCreature casterCreature) return 1;

        TouchAttackResult result = casterCreature.TouchAttackRanged(targetCreature, true);

        return result switch
        {
            TouchAttackResult.Hit => hit,
            TouchAttackResult.CriticalHit => criticalHit,
            _ => miss
        };
    }

    /// <summary>
    ///     A nice looking string color for warlock
    /// </summary>
    public static string String(string message) => NwEffects.ColorString(message, rgb: "517");
}