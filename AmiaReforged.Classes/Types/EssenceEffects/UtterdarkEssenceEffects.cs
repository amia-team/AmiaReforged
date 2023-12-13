using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class UtterdarkEssenceEffects : EssenceEffectApplier
{
    public UtterdarkEssenceEffects(uint target, uint caster) : base(target, caster)
    {
    }

    public override void ApplyEffects(int damage)
    {
        bool resistSpell = NwEffects.ResistSpell(Caster, Target);
        if (resistSpell) return;
        if (NwEffects.HasMantle(Target) && !resistSpell)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SPELL_MANTLE_USE), Target);
            ObjectPlugin.DoSpellLevelAbsorption(Target, Caster);
            return;
        }

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_NEGATIVE), Target);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_NEGATIVE_ENERGY), Target);

        bool passedFortSave = FortitudeSave(Target, CalculateDC(), SAVING_THROW_TYPE_NEGATIVE, Caster) == TRUE;

        if (passedFortSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_FORTITUDE_SAVING_THROW_USE), Target);
            return;
        }
        if (!passedFortSave)
        {
            int warlockLevels = GetLevelByClass(57, Caster);
            float essenceDuration = warlockLevels < 5 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 5);
            IntPtr essenceEffect = NwEffects.LinkEffectList(new List<IntPtr>
            {
                SupernaturalEffect(EffectNegativeLevel(2)),
                EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE)
            });
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, essenceEffect, Target, essenceDuration);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_REDUCE_ABILITY_SCORE), Target);
        }
    }
}