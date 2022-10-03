using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class IncandescentOnExit
{
    public void RemoveIncandescentEffects()
    {
        uint creature = GetExitingObject();

        IntPtr effect = GetFirstEffect(creature);

        while (GetIsEffectValid(effect) == TRUE)
        {
            if(GetEffectTag(effect) == "incand_negfx") RemoveEffect(creature, effect);

            effect = GetNextEffect(creature);
        }
    }
}