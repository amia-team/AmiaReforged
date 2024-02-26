using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class ScreamingEssenceEffects : EssenceEffectApplier
{
    public ScreamingEssenceEffects(uint target, uint caster) : base(target, caster)
    {
    }

    public override void ApplyEffects(int damage)
    {
        bool resistSpell = NwEffects.ResistSpell(Caster, Target);
        if (resistSpell) return;
        if (NwEffects.HasMantle(Target) && !resistSpell)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SPELL_MANTLE_USE), Target);
            ObjectPlugin.DoSpellLevelAbsorption(Target, Caster, -1, 9, 0);
            return;
        }

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_SONIC), Target);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SONIC), Target);

        bool passedFortSave = FortitudeSave(Target, CalculateDC(), SAVING_THROW_TYPE_SONIC, Caster) == TRUE;

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
                EffectDeaf(),
                EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE)
            });
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, essenceEffect, Target, essenceDuration);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SILENCE), Target);
        }
    }
}