﻿using AmiaReforged.Classes.EffectUtils;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Types.EssenceEffects;

public class FrightfulEssenceEffects : EssenceEffectApplier
{
    public FrightfulEssenceEffects(uint target, uint caster) : base(target, caster)
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

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), Target);

        if (WillSave(Target, CalculateDc(), SAVING_THROW_TYPE_MIND_SPELLS) == TRUE) return;
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectFrightened(), Target, RoundsToSeconds(2));
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PDK_FEAR), Target, RoundsToSeconds(2));
    }
}