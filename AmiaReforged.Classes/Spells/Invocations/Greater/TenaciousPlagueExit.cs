using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class TenaciousPlagueExit
{
    public int Run(uint nwnObjectId)
    {
        uint exitingObject = GetExitingObject();
        IntPtr effect = GetFirstEffect(exitingObject);

        while (GetIsEffectValid(effect) == TRUE)
        {
            if (GetEffectTag(effect) == "tenacious_plague") RemoveEffect(exitingObject, effect);

            effect = GetNextEffect(exitingObject);
        }

        return 0;
    }
}