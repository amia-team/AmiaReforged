using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class IncandescentHeartbeat
{
    public void Heartbeat(uint nwnObjectId)
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
            damage = GetRacialType(current) == RACIAL_TYPE_UNDEAD ? damage : damage / 2;

            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage, DAMAGE_TYPE_POSITIVE), current);

            current = GetNextInPersistentObject(nwnObjectId);
        }
    }
}