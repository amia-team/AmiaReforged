using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class ChillingTentaclesHeartbeat
{
    public int Run(uint nwnObjectId)
    {
        uint target = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int spellId = GetSpellId();

        while (GetIsObjectValid(target) == TRUE)
        {
            bool notValidTarget = target == caster ||
                                  GetIsFriend(target, caster) == TRUE;
            if (notValidTarget)
            {
                target = GetNextInPersistentObject(nwnObjectId);
                continue;
            }

            SignalEvent(target, EventSpellCastAt(nwnObjectId, spellId));
            EldritchTentacle.StrikeTargetWithTentacle(target, caster);

            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(d6(2), DAMAGE_TYPE_COLD), target);

            target = GetNextInPersistentObject(nwnObjectId);
        }

        return 0;
    }
}