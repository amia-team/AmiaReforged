using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class ChillingTentaclesHeartbeat
{
    public void ChillingTentaclesHeartbeatEffects(uint nwnObjectId)
    {
        uint target = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);

        while (GetIsObjectValid(target) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(target, 2, caster))
            {
                SignalEvent(target, EventSpellCastAt(nwnObjectId, GetSpellId()));
                EldritchTentacle.StrikeTargetWithTentacle(target, caster);

                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(d6(2), DAMAGE_TYPE_COLD), target);
            }

            target = GetNextInPersistentObject(nwnObjectId);
        }
    }
}