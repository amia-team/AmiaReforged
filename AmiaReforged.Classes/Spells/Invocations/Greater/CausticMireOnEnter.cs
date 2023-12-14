using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class CausticMireOnEnter
{
    private uint _caster;
    public void ApplyOnEnterEffects(NwObject aoe)
    {
        _caster = GetAreaOfEffectCreator(aoe);
        
        uint enteringObject = GetEnteringObject();

        bool notValidTarget = enteringObject == _caster ||
                              GetIsFriend(enteringObject, _caster) == TRUE;
        if (notValidTarget) return;
        if (NwEffects.ResistSpell(_caster, enteringObject)) return;

        int spellId = GetSpellId();
        SignalEvent(enteringObject, EventSpellCastAt(_caster, spellId));
        int damage = CalculateDamage(enteringObject);

        ApplyEffectToObject(DURATION_TYPE_PERMANENT, TagEffect(EffectMovementSpeedDecrease(50), "mire_slow"),
            enteringObject);

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_ACID), enteringObject);

        ApplyEffectToObject(DURATION_TYPE_PERMANENT,
            TagEffect(EffectDamageImmunityDecrease(DAMAGE_TYPE_FIRE, 10), "mire_sludge"), enteringObject);
    }
    
    private int CalculateDamage(uint enteringObject)
    {
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, _caster);
        int damageCalculation = casterChaMod < 10 ? d6() + casterChaMod : d6() + 10;
        return damageCalculation;
    }
}