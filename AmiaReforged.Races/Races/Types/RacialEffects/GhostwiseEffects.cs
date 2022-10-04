using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects
{
    public class GhostwiseEffects : IEffectCollector
    {
        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {
            return new List<IntPtr>
            {
                NWScript.EffectSkillDecrease(NWScript.SKILL_SPOT, 2),
                NWScript.EffectSkillDecrease(NWScript.SKILL_CONCENTRATION, 2),
                NWScript.EffectSkillIncrease(NWScript.SKILL_ANIMAL_EMPATHY, 2)
            };
        }
    }
}