using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class CausticMireHeartbeat
{
    public void CausticMireHeartbeatEffects(uint nwnObjectId)
    {
        uint currentTarget = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(currentTarget, 2, caster))
            {
                SignalEvent(currentTarget, EventSpellCastAt(nwnObjectId, GetSpellId()));

                if (NwEffects.ResistSpell(caster, currentTarget))
                {
                    currentTarget = GetNextInPersistentObject(nwnObjectId);
                    continue;
                }

                int damage = casterChaMod > 10 ? d6() + 10 : d6() + casterChaMod;

                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_ACID), currentTarget);
            }

            currentTarget = GetNextInPersistentObject(nwnObjectId);
        }
    }
}