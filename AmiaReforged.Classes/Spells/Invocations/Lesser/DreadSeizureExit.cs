using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class DreadSeizureExit
{
    public int DreadSeizureExitEffects(uint nwnObjectId)
    {
        uint exitingObject = GetExitingObject();
        IntPtr debuffs = GetFirstEffect(exitingObject);

        while (GetIsEffectValid(debuffs) == TRUE)
        {
            if (GetEffectTag(debuffs) == "dreadseizure"){
                RemoveEffect(exitingObject, debuffs);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE), exitingObject, 0.1f);
            }

            debuffs = GetNextEffect(exitingObject);
        }
        return 0;
    }
}