using AmiaReforged.Races.Races.Script.Types;
using NLog;
using static NWN.Core.NWScript;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class HalfDragonEffects : IEffectCollector
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const int Heritage = 1238;
    private bool _hasHeritageFeat;
    private uint _oid = OBJECT_INVALID;

    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        Log.Info("------------------------JES LOOK HERE!!!!------------------------");
        Log.Info("------------------------JES LOOK HERE!!!!------------------------");
        Log.Info("------------------------JES LOOK HERE!!!!------------------------");
        Log.Info("------------------------JES LOOK HERE!!!!------------------------");
        Log.Info($"{GetName(objectId)} is a draconic race.");
        Log.Info($"Gathering effects for object {GetName(objectId)}."); 
            
        _oid = objectId;
        _hasHeritageFeat = HasHeritageFeat();
        WriteTimestampedLogEntry($"Has heritage feat: {_hasHeritageFeat}");
            
        int spellResistance = GetSpellResistanceBasedOnFeat();
        Log.Info($"SpellResistance: {spellResistance}");
            
        List<IntPtr> effectsForObject = new()
        {
            EffectSpellResistanceIncrease(spellResistance)
        };

        AddHeritageEffectsIfObjectHasFeat(effectsForObject);
        return effectsForObject;
    }

    private bool HasHeritageFeat()
    {
        return GetHasFeat(Heritage, _oid) == 1;
    }

    private int GetSpellResistanceBasedOnFeat()
    {
        int hitDice = GetHitDice(_oid);
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

        Log.Info("Adding heritage effects.");
        effectsForObject.Add(EffectAttackDecrease(1));
        effectsForObject.Add(EffectSavingThrowDecrease(SAVING_THROW_ALL, 1));
    }
}