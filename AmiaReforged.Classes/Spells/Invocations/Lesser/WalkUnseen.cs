using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

public class WalkUnseen
{
    public int Run(uint nwnObjectId)
    {
        float duration = TurnsToSeconds(GetCasterLevel(nwnObjectId));

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectInvisibility(INVISIBILITY_TYPE_NORMAL), nwnObjectId,
            duration);
        return 0;
    }
}