using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects
{
    public class OrogEffects : IEffectCollector
    {
        private const int Heritage = 1238;
        private bool _hasHeritageFeat;
        private uint _oid;

        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {
            _oid = objectId;
            _hasHeritageFeat = HasHeritageFeat();

            List<IntPtr>? orogEffects = new()
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_CRAFT_ARMOR, 2),
                NWScript.EffectSkillIncrease(NWScript.SKILL_CRAFT_WEAPON, 2),
                NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_FIRE, 5),
                NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_COLD, 5)
            };

            AddHeritageEffectsIfObjectHasFeat(orogEffects);

            return orogEffects;
        }

        private bool HasHeritageFeat()
        {
            return NWScript.GetHasFeat(Heritage, _oid) == 1;
        }

        private void AddHeritageEffectsIfObjectHasFeat(List<IntPtr> effectsForObject)
        {
            if (!_hasHeritageFeat) return;

            effectsForObject.Add(NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_ALL, 1));
            effectsForObject.Add(NWScript.EffectACIncrease(2));
        }
    }
}