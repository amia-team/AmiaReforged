﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Strength)]
public class StrengthCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int strMod = playerCreature.GetAbilityModifier(Ability.Strength);
        
        int result = roll + strMod;
        
        string message = $"[?] Strength Check = D20: {roll} + Strength Modifier ( {strMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}