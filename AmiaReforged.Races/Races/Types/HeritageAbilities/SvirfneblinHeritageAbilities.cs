﻿using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace Amia.Racial.Races.Types.HeritageAbilities
{
    public class SvirfneblinHeritageAbilities : IHeritageAbilities
    {
        public void SetupStats(NwPlayer player)
        {
            CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_WISDOM, 2);
        }
    }
}