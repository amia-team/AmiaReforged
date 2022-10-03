using NWN.Amia.Main.Managed.Feats.Types;
using NWN.Core;

namespace Amia.Racial.Races.Types.RacialEffects
{
    public class KoboldEffects : IEffectCollector
    {
        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {
            return new List<IntPtr>
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_SET_TRAP, 4),
                NWScript.EffectSkillIncrease(NWScript.SKILL_SEARCH, 4)
            };
        }
    }
}