﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Lore)]
public class LoreSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int loreMod = playerCreature.GetSkillRank(Skill.Lore!);
        
        int result = roll + loreMod;
        
        playerCreature.SpeakString(new SkillCheckString("Lore", roll, loreMod, result).GetRollResult());
    }
}