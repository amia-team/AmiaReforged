using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.Shapes;

/// <summary>
///     Static class responsible for calculating damage done by Eldritch blasts.
/// </summary>
public static class EldritchDamage
{
    public static int CalculateDamageAmount(uint caster)
    {
        int damageDice = d6((int)(GetLevelByClass(57, caster) / 2 + ExtraDieFromFeats(caster)));
        int extraDamageFromEldritchMaster = GetHasFeat(1298, caster) == TRUE ? (int)(damageDice * 0.5) : 0;
        return damageDice + extraDamageFromEldritchMaster;
    }

    private static int ExtraDieFromFeats(uint caster)
    {
        int highestLevel = CreaturePlugin.GetHighestLevelOfFeat(caster, 1300);

        int extraDie = highestLevel switch
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

        return extraDie;
    }
}