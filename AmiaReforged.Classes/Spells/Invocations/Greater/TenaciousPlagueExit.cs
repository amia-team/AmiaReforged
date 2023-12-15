using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class TenaciousPlagueExit
{
    public void TenaciousPlagueExitEffects(uint nwnObjectId)
    {
        uint exitingObject = GetExitingObject();
        IntPtr effect = GetFirstEffect(exitingObject);

        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectTag(effect) == "tenacious_plague") RemoveEffect(exitingObject, effect);

            effect = GetNextEffect(exitingObject);
        }
    }
}