using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class HobgoblinEffects : IEffectCollector
{
    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        return new()
        {
            NWScript.EffectSkillIncrease(NWScript.SKILL_MOVE_SILENTLY, 4)
        };
    }
}