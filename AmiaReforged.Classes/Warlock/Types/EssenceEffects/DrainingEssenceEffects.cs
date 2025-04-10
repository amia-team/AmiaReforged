﻿using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Warlock.Types.EssenceEffects;

public class DrainingEssenceEffects : EssenceEffectApplier
{
    public DrainingEssenceEffects(uint target, uint caster) : base(target, caster)
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

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), Target);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_MAGBLUE), Target);

        bool passedWillSave = WillSave(Target, CalculateDc(), SAVING_THROW_TYPE_SPELL, Caster) == TRUE;

        if (passedWillSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), Target);
            return;
        }

        if (!passedWillSave)
        {
            int warlockLevels = GetLevelByClass(57, Caster);
            float essenceDuration = warlockLevels < 10 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 10);
            IntPtr essenceEffect = NwEffects.LinkEffectList(new List<IntPtr>
            {
                EffectSlow(),
                EffectVisualEffect(VFX_DUR_CESSATE_NEGATIVE)
            });
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, essenceEffect, Target, essenceDuration);
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SLOW), Target);
        }
    }
}