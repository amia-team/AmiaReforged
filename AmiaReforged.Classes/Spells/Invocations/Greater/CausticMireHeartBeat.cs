using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class CausticMireHeartBeat
{
    public void Run(uint nwnObjectId)
    {
        uint current = GetFirstInPersistentObject(nwnObjectId);
        uint areaOfEffectCreator = GetAreaOfEffectCreator(nwnObjectId);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, areaOfEffectCreator);
        int spellId = GetSpellId();

        while (GetIsObjectValid(current) == TRUE)
        {
            bool notValidTarget = current == areaOfEffectCreator ||
                                  GetIsFriend(current, areaOfEffectCreator) == TRUE;
            if (notValidTarget)
            {
                current = GetNextInPersistentObject(nwnObjectId);
                continue;
            }

            if (NwEffects.ResistSpell(areaOfEffectCreator, current))
            {
                current = GetNextInPersistentObject(nwnObjectId);
                continue;
            }

            SignalEvent(current, EventSpellCastAt(nwnObjectId, spellId));
            int damage = casterChaMod > 10 ? d6() + 10 : d6() + casterChaMod;

            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_ACID), current);

            current = GetNextInPersistentObject(nwnObjectId);
        }
    }
}