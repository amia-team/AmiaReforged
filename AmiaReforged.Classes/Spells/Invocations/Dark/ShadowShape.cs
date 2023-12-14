using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class ShadowShape
{
    public int Run(uint nwnObjectId)
    {
        float duration = TurnsToSeconds(GetCasterLevel(nwnObjectId));
        IntPtr linkedConceal = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectVisualEffect(VFX_DUR_INVISIBILITY),
            EffectVisualEffect(VFX_DUR_PROT_SHADOW_ARMOR),
            EffectSavingThrowIncrease(SAVING_THROW_ALL, 4, SAVING_THROW_TYPE_DEATH),
            EffectSavingThrowIncrease(SAVING_THROW_ALL, 4, SAVING_THROW_TYPE_NEGATIVE),
            EffectConcealment(50)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, linkedConceal, nwnObjectId, duration);
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectInvisibility(INVISIBILITY_TYPE_NORMAL), nwnObjectId,
            duration);

        return 0;
    }
}