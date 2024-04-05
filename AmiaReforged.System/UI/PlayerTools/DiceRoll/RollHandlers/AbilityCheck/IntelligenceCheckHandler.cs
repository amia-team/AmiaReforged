﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Intelligence)]
public class IntelligenceCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int intMod = playerCreature.GetAbilityModifier(Ability.Intelligence);
        
        playerCreature.SpeakString(new AbilityCheckString("Intelligence", roll, intMod).GetAbilityCheckString());
    }
}