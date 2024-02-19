﻿using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Types.HeritageAbilities;

public class FeyriHeritageAbilities : IHeritageAbilities
{
    public void SetupStats(NwPlayer player)
    {
        CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_INTELLIGENCE, 2);
    }
}