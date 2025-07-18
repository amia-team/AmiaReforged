﻿using AmiaReforged.Races.Races.Script.Types;
using NWN.Core;

namespace AmiaReforged.Races.Races.Types.RacialEffects;

public class FfolkEffects : IEffectCollector
{
    public List<IntPtr> GatherEffectsForObject(uint objectId)
    {
        return new List<IntPtr>
        {
            NWScript.EffectSkillIncrease(NWScript.SKILL_ANIMAL_EMPATHY, 2),
            NWScript.EffectSkillIncrease(NWScript.SKILL_LORE, 2)
        };
    }
}