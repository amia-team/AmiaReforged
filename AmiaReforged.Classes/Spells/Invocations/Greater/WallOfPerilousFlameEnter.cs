using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WallOfPerilousFlameOnEnter
{
    public void Run(uint nwnObjectId)
    {
        uint enteringObject = GetEnteringObject();
        uint caster = GetAreaOfEffectCreator(nwnObjectId);

        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);

        bool notValidTarget = enteringObject == caster ||
                              GetIsFriend(enteringObject, caster) == TRUE;

        if (notValidTarget) return;
        if (NwEffects.ResistSpell(caster, enteringObject)) return;

        int spellId = GetSpellId();
        SignalEvent(enteringObject, EventSpellCastAt(nwnObjectId, spellId));

        int damage = d12() + casterChaMod;
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_FIRE),
            enteringObject);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), enteringObject);
    }
}