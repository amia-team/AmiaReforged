using NWN.Amia.Main.Managed.Feats.Types;
using NWN.Core;

namespace Amia.Racial.Races.Types.RacialEffects
{
    public class CentaurEffects : IEffectCollector
    {
        private uint _oid = NWScript.OBJECT_INVALID;

        public List<IntPtr> GatherEffectsForObject(uint objectId)
        {
            _oid = objectId;

            List<IntPtr>? effects = new List<IntPtr>
            {
                NWScript.EffectSkillIncrease(NWScript.SKILL_MOVE_SILENTLY, 2),
            };


            return effects;
        }

    }
}