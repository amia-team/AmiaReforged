using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WallOfPerilousFlameOnEnter
{
    public void WallOfFlameEnterEffects(uint nwnObjectId)
    {
        uint enteringObject = GetEnteringObject();
        uint caster = GetAreaOfEffectCreator(nwnObjectId);

        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);

        if (!NwEffects.IsValidSpellTarget(enteringObject, 2, caster)) return;

        SignalEvent(enteringObject, EventSpellCastAt(nwnObjectId, GetSpellId()));

        if (NwEffects.ResistSpell(caster, enteringObject)) return;

        int damage = d12() + casterChaMod;
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_FIRE),
            enteringObject);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), enteringObject);
    }
}