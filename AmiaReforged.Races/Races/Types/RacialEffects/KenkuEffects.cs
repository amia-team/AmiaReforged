using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

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