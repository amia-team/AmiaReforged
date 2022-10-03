using NWN.Amia.Main.Managed.Feats.Types;
using NWN.Core;

namespace Amia.Racial.Races.Types.RacialEffects
{
    public class GoblinEffects : IEffectCollector
    {
        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {
            return new()
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_MOVE_SILENTLY, 2),
                NWScript.EffectSkillIncrease(NWScript.SKILL_DISCIPLINE, 2)
            };
        }
    }
}