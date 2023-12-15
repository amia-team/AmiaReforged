using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WallOfPerilousFlameHeartbeat
{
    public void WallOfFlameHeartbeatEffects(uint nwnObjectId)
    {
        uint current = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);

        while (GetIsObjectValid(current) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(current, 2, caster))
            {
                SignalEvent(current, EventSpellCastAt(nwnObjectId, GetSpellId()));

                if (NwEffects.ResistSpell(caster, current))
                {
                    current = GetNextInPersistentObject(nwnObjectId);
                    continue;
                }

                int damage = d12() + casterChaMod;
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_FIRE),
                    current);
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), current);
            }

            current = GetNextInPersistentObject(nwnObjectId);
        }
    }
}