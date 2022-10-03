using NWN.Core;

namespace AmiaReforged.Classes.Spells.Invocations.Least;

public class BoundOnesLuck
{
    public int Run(uint nwnObjectId)
    {
        int chmod = NWScript.GetAbilityModifier(NWScript.ABILITY_CHARISMA, nwnObjectId);

        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY,
            NWScript.EffectSavingThrowIncrease(NWScript.SAVING_THROW_ALL, chmod), nwnObjectId,
            NWScript.HoursToSeconds(NWScript.GetCasterLevel(nwnObjectId)));

        return 0;
    }
}