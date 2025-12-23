﻿using Anvil.API;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Races.Races.Types.HeritageAbilities;

public class FeytouchedHeritageAbilities : IHeritageAbilities
{
    public void SetupStats(NwPlayer player)
    {
        CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_DEXTERITY, 2);
    }

    public void RemoveStats(NwPlayer player)
    {
        CreaturePlugin.ModifyRawAbilityScore(player.LoginCreature, NWScript.ABILITY_DEXTERITY, -2);
    }
}
