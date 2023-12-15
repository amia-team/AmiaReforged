using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Dark;

public class WordOfChanging
{
    public void CastWordOfChanging(uint nwnObjectId)
    {
        int casterLevel = GetCasterLevel(nwnObjectId);
        int ab = casterLevel / 4 > 5 ? 5 : casterLevel / 4;
        float duration = RoundsToSeconds(casterLevel);

        uint pcKey = GetItemPossessedBy(nwnObjectId, "ds_pckey");

        // Sets custom shape if the pcKey holds the required variables for SetCustomShape.
        NwEffects.SetCustomShape(nwnObjectId, pcKey, "woc", duration);

        IntPtr wordOfChanging = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectAttackIncrease(ab),
            EffectAbilityIncrease(ABILITY_STRENGTH, d4()),
            EffectAbilityIncrease(ABILITY_CONSTITUTION, d4()),
            EffectAbilityIncrease(ABILITY_DEXTERITY, d4()),
            EffectTemporaryHitpoints(d6(casterLevel)),
            EffectSpellFailure()
        });

        // If doesn't have a custom shape for WoC, then applies a magenta hue to denote the effect.
        if (GetLocalInt(pcKey, "has_custom_woc_shape") == FALSE)
            EffectLinkEffects(EffectVisualEffect(VFX_DUR_AURA_MAGENTA), wordOfChanging);

        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, wordOfChanging, nwnObjectId, duration);
    }
}
