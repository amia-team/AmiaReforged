using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class AasimarEffects : IEffectCollector
{
    private const int Heritage = 1238;
    private bool _hasHeritageFeat;
    private uint _oid = NWScript.OBJECT_INVALID;

    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        _oid = objectId;
        _hasHeritageFeat = HasHeritageFeat();

        List<IntPtr>? effects = new List<IntPtr>
        {
            NWScript.EffectSkillIncrease(NWScript.SKILL_PERSUADE, 2),
            NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_COLD, 5),
            NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 5),
            NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_ELECTRICAL, 5)
        };

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