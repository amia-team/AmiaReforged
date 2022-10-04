using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects
{
    public class OrcEffects : IEffectCollector
    {
        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {
            return new()
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_INTIMIDATE, 4),
                NWScript.EffectSkillIncrease(NWScript.SKILL_DISCIPLINE, 4)
            };
        }
    }
}