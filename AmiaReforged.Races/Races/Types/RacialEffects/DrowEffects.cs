using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class DrowEffects : IEffectCollector
{
    private const int Heritage = 1238;
    private bool _hasHeritageFeat;
    private uint _oid = NWScript.OBJECT_INVALID;

    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        _oid = objectId;
        _hasHeritageFeat = HasHeritageFeat();

        int spellResistance = GetSpellResistanceBasedOnFeat();

        List<IntPtr> effectsForObject = new()
        {
            NWScript.EffectSpellResistanceIncrease(spellResistance)
        };

        AddHeritageEffectsIfObjectHasFeat(effectsForObject);
        return effectsForObject;
    }

    private bool HasHeritageFeat()
    {
        return NWScript.GetHasFeat(Heritage, _oid) == 1;
    }

    private int GetSpellResistanceBasedOnFeat()
    {
        int hitDice = NWScript.GetHitDice(_oid);

        return _hasHeritageFeat
            ? SpellResistanceWithFeat(hitDice)
            : SpellResistanceWithoutFeat(hitDice);
    }


    private static int SpellResistanceWithFeat(int hitDice)
    {
        return hitDice + 4;
    }

    private static int SpellResistanceWithoutFeat(in int hitDice)
    {
        return hitDice - 2;
    }

    private void AddHeritageEffectsIfObjectHasFeat(ICollection<IntPtr> effectsForObject)
    {
        if (!_hasHeritageFeat) return;

        effectsForObject.Add(NWScript.EffectAttackDecrease(1));
        effectsForObject.Add(NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_ALL, 1));
    }
}