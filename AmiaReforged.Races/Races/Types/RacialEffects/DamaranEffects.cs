using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class DamaranEffects : IEffectCollector
{
    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        return new()
        {
            NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_FORT, 1)
        };
    }
}