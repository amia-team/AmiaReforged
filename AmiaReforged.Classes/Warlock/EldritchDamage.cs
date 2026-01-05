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
        // 1. Gather stats
        int warlockLevels = GetLevelByClass(57, caster);
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int epicBlastCount = ExtraDieFromFeats(caster);

        // 2. Base damage roll
        int damage = d2(warlockLevels);

        // 3. Epic Eldritch Blast: +5 flat damage per feat
        damage += epicBlastCount * 5;

        // 4. Eldritch Master: +25% damage
        if (GetHasFeat(1298, caster) == TRUE)
        {
            damage += damage / 4;
        }

        // 5. Charisma scaling: +1% per CHA mod
        int chaPercent = chaMod;

        // Level 30 capstone: CHA mod × 1.5
        if (warlockLevels == 30)
        {
            chaPercent = (int)(chaMod * 1.5);
        }

        damage += (damage * chaPercent) / 100;

        return damage;
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
