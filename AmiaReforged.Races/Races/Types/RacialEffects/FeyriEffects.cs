using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class FeyriEffects : IEffectCollector
{
    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        return new List<IntPtr>
        {
            NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 10)
        };
    }
}