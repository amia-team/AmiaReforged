using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class TenaciousPlagueHeartbeat
{
    public int Run(uint nwnObjectId)
    {
        uint current = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int spellId = GetSpellId();

        while (GetIsObjectValid(current) == TRUE)
        {
            bool notValidTarget = current == caster ||
                                  GetIsFriend(current, caster) == TRUE;
            if (notValidTarget)
            {
                current = GetNextInPersistentObject(nwnObjectId);
                continue;
            }

            SignalEvent(current, EventSpellCastAt(nwnObjectId, spellId));
            int damage = casterChaMod < 10 ? d12() + casterChaMod : d12() + 10;
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), current);

            current = GetNextInPersistentObject(nwnObjectId);
        }

        return 0;
    }
}