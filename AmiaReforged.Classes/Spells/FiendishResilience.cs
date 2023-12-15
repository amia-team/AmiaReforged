using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells;

public class FiendishResilience
{
    public void Run(uint nwnObjectId)
    {
        float duration = RoundsToSeconds(GetCasterLevel(nwnObjectId));
        int warlockLevels = GetLevelByClass(57, nwnObjectId);

        int regenAmount = warlockLevels switch
        {
            >= 8 and < 13 => 1,
            >= 13 and < 18 => 2,
            >= 18 => 5,
            _ => 1
        };
        
        IntPtr fiendishResilience = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectVisualEffect( VFX_DUR_AURA_PULSE_ORANGE_WHITE),
            EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE)
        });

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, SupernaturalEffect(EffectRegenerate(regenAmount, 6.0f)),
            nwnObjectId, duration);
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, fiendishResilience, nwnObjectId, duration);
    }
}