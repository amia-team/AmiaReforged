using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class CausticMireOnEnter
{
    public void CausticMireEnterEffects(NwObject aoe)
    {
        uint caster = GetAreaOfEffectCreator(aoe);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int damage = casterChaMod < 10 ? d6() + casterChaMod : d6() + 10;

        uint enteringObject = GetEnteringObject();

        if (!NwEffects.IsValidSpellTarget(enteringObject, 2, caster)) return;
        SignalEvent(enteringObject, EventSpellCastAt(caster, GetSpellId()));
        if (NwEffects.ResistSpell(caster, enteringObject)) return;

        ApplyEffectToObject(DURATION_TYPE_PERMANENT, TagEffect(EffectMovementSpeedDecrease(50), sNewTag: "mire_slow"),
            enteringObject);

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_ACID), enteringObject);

        ApplyEffectToObject(DURATION_TYPE_PERMANENT,
            TagEffect(EffectDamageImmunityDecrease(DAMAGE_TYPE_FIRE, 10), sNewTag: "mire_sludge"), enteringObject);
    }
}