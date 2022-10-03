using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class IncandescentOnEnter
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

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_POSITIVE), enteringObject);

        ApplyAntiUndeadEffects(enteringObject);
    }

    private int CalculateDamage(uint enteringObject)
    {
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, _caster);
        int damage = chaMod > 10 ? d6() + 10 : d6() + chaMod;

        damage = GetRacialType(enteringObject) == RACIAL_TYPE_UNDEAD ? damage : damage / 2;
        return damage;
    }

    private static void ApplyAntiUndeadEffects(uint enteringObject)
    {
        bool undead = GetRacialType(enteringObject) == RACIAL_TYPE_UNDEAD;

        if (!undead) return;
        IntPtr linkedEffects = NwEffects.LinkEffectList(
            new List<IntPtr>
            {
                EffectACDecrease(2),
                EffectAttackDecrease(2),
                EffectTurnResistanceDecrease(4)
            });

        ApplyEffectToObject(DURATION_TYPE_PERMANENT, TagEffect(SupernaturalEffect(linkedEffects), "incand_negfx"),
            enteringObject);
    }
}