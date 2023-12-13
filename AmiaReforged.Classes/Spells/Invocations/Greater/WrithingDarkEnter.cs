﻿using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WrithingDarkEnter
{
    public void WrithingDarkEnterEffects(uint nwnObjectId)
    {
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int damage = chaMod > 10 ? d6() + 10 : d6() + chaMod;

        uint enteringObject = GetEnteringObject();

        if (!NwEffects.IsValidSpellTarget(enteringObject, 2, caster)) return;

        SignalEvent(enteringObject, EventSpellCastAt(caster, 998));

        if (NwEffects.ResistSpell(caster, enteringObject)) return;

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), enteringObject);

        bool passedWillSave = WillSave(enteringObject, NwEffects.CalculateDC(caster), 0, caster) == TRUE;
        if (passedWillSave) ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), enteringObject);
        if (!passedWillSave)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBlindness(), enteringObject, 6f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BLIND), enteringObject, 6f);
        }
    }
}