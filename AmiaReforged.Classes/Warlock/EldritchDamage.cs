using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock;

/// <summary>
///     Static class responsible for calculating damage done by Eldritch blasts.
/// </summary>
public static class EldritchDamage
{
    public static int CalculateDamageAmount(uint caster)
    {
        int warlockLevels = GetLevelByClass(57, caster);
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int damageBonus = (int)(warlockLevels == 30 ? chaMod * 1.5 : chaMod);
        int epicBlastBonus = 5 * ExtraDieFromFeats(caster);
        int damageDice = d2(warlockLevels) + epicBlastBonus;

        int chaBonusDamage = damageBonus * damageBonus / 100;
        int extraDamageFromEldritchMaster = GetHasFeat(1298, caster) == TRUE ? (int)(damageDice * 0.25) : 0;

        damageDice += chaBonusDamage;
        damageDice += extraDamageFromEldritchMaster;

        return damageDice;
    }

    private static int ExtraDieFromFeats(uint caster)
    {
        int highestLevel = CreaturePlugin.GetHighestLevelOfFeat(caster, 1300);

        return highestLevel switch
        {
            1300 => 1,
            1301 => 2,
            1302 => 3,
            1303 => 4,
            1304 => 5,
            1305 => 6,
            1306 => 7,
            _ => 0
        };
    }
}
