using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class AvarielEffects : IEffectCollector
{
    private const int Heritage = 1238;
    private bool _hasHeritageFeat;
    private uint _oid = NWScript.OBJECT_INVALID;

    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        _oid = objectId;
        _hasHeritageFeat = HasHeritageFeat();

        List<IntPtr>? effects = new();
           
        AddHeritageEffectsIfObjectHasFeat(effects);

        return effects;
    }

    private bool HasHeritageFeat()
    {
        return NWScript.GetHasFeat(Heritage, _oid) == 1;
    }

    private void AddHeritageEffectsIfObjectHasFeat(ICollection<IntPtr> effectsForObject)
    {
        if (!_hasHeritageFeat) return;

        effectsForObject.Add(NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_ALL, 1));
    }
}