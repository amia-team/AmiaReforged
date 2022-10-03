using NWN.Amia.Main.Managed.Feats.Types;
using NWN.Core;

namespace Amia.Racial.Races.Types.RacialEffects
{
    public class KenkuEffects : IEffectCollector
    {
        public List<IntPtr> GatherEffectsForObject(uint objectId) {
            return new List<IntPtr>
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_HIDE, 2),
                NWScript.EffectSkillIncrease(NWScript.SKILL_MOVE_SILENTLY, 2)
            };
        }
    }
}