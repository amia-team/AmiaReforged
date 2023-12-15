using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class TenaciousPlagueHeartbeat
{
    public void TenaciousPlagueHeartbeatEffects(uint nwnObjectId)
    {
        uint current = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);

        while (GetIsObjectValid(current) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(current, 2, caster))
            {
                SignalEvent(current, EventSpellCastAt(nwnObjectId, GetSpellId()));
                int damage = casterChaMod < 10 ? d12() + casterChaMod : d12() + 10;
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), current);
            }

            current = GetNextInPersistentObject(nwnObjectId);
        }
    }
}