using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class CausticMireOnExit
{
    public void CausticMireExitEffects(uint nwnObjectId)
    {
        uint exitingObject = GetExitingObject();
        IntPtr effect = GetFirstEffect(exitingObject);

        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectTag(effect) == "mire_slow") RemoveEffect(exitingObject, effect);

            if (GetEffectTag(effect) == "mire_sludge") RemoveEffect(exitingObject, effect);

            effect = GetNextEffect(exitingObject);
        }
    }
}