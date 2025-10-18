using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.Shapes;

/// <summary>
///     Static class responsible for calculating damage done by Eldritch blasts.
/// </summary>
public static class EldritchDamage
{
    public static int CalculateDamageAmount(uint caster)
    {
        int warlockLevels = GetLevelByClass(57, caster);
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        // At most 25% extra damage at your capstone
        int damageBonus = (int)(warlockLevels == 30 ? Math.Min(25, chaMod * 1.5) : chaMod);
        if (warlockLevels / 2 < chaMod && warlockLevels < 30) damageBonus = warlockLevels / 2;
        int damageDice = d2(warlockLevels) + d6(ExtraDieFromFeats(caster));
        damageDice += damageDice * damageBonus / 100;
        int extraDamageFromEldritchMaster = GetHasFeat(1298, caster) == TRUE ? (int)(damageDice * 0.25) : 0;
        return damageDice + extraDamageFromEldritchMaster;
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
