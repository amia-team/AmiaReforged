using NWN.Amia.Main.Managed.Feats.Types;
using NWN.Core;

namespace Amia.Racial.Races.Types.RacialEffects
{
    public class DuergarEffects : IEffectCollector
    {
        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {

            List<IntPtr> effects = new()
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_MOVE_SILENTLY, 4),
                NWScript.EffectSkillIncrease(NWScript.SKILL_LISTEN, 1),
                NWScript.EffectSkillIncrease(NWScript.SKILL_SPOT, 1),
                NWScript.EffectImmunity(NWScript.IMMUNITY_TYPE_PARALYSIS),
                NWScript.EffectImmunity(NWScript.IMMUNITY_TYPE_POISON),
                NWScript.EffectSpellImmunity(NWScript.SPELL_PHANTASMAL_KILLER)
            };


            return effects;
        }

    }
}