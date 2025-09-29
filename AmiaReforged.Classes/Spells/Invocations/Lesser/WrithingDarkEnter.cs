using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

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
        if (GetHasSpellEffect(EFFECT_TYPE_ULTRAVISION, enteringObject) == TRUE ||
            GetHasSpellEffect(EFFECT_TYPE_TRUESEEING, enteringObject) == TRUE) return;

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), enteringObject);

        bool passedWillSave = WillSave(enteringObject, WarlockUtils.CalculateDc(caster), 0, caster) == TRUE;
        if (passedWillSave)
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE),
                enteringObject);
        if (!passedWillSave)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBlindness(), enteringObject, 6f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BLIND), enteringObject, 6f);
        }
    }
}