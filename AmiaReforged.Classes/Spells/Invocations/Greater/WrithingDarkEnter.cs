using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WrithingDarkEnter
{
    public void Run(uint nwnObjectId)
    {
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int chaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);

        uint enteringObject = GetEnteringObject();

        bool notValidTarget = enteringObject == caster ||
                              GetIsFriend(enteringObject, caster) == TRUE;
        if (notValidTarget) return;
        if (NwEffects.ResistSpell(caster, enteringObject)) return;

        int spellId = GetSpellId();
        SignalEvent(enteringObject, EventSpellCastAt(nwnObjectId, spellId));
        int damage = chaMod > 10 ? d6() + 10 : d6() + chaMod;
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), enteringObject);
    }
}