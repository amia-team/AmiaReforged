using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class WrithingDarkHeartbeat
{
    public void WrithingDarkHeartbeatEffects(uint nwnObjectId)
    {
        uint current = GetFirstInPersistentObject(nwnObjectId);
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        int casterChaMod = GetAbilityModifier(ABILITY_CHARISMA, caster);
        int damage = casterChaMod > 10 ? d6() + 10 : d6() + casterChaMod;

        while (GetIsObjectValid(current) == TRUE)
        {
            if (NwEffects.IsValidSpellTarget(current, 2, caster))
            {
                SignalEvent(current, EventSpellCastAt(nwnObjectId, 998));

                if (NwEffects.ResistSpell(caster, current))
                {
                    current = GetNextInPersistentObject(nwnObjectId);
                    continue;
                }

                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(damage), current);

                bool passedWillSave = WillSave(current, NwEffects.CalculateDC(caster), 0, caster) == TRUE;

                if (passedWillSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), current);
                    current = GetNextInPersistentObject(nwnObjectId);
                    continue;
                }
                if (!passedWillSave)
                {
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBlindness(), current, 6f);
                    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BLIND), current, 6f);
                }
            }

            current = GetNextInPersistentObject(nwnObjectId);
        }
    }
}