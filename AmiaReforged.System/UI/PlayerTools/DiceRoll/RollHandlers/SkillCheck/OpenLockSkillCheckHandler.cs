﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.OpenLock)]
public class OpenLockSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int openLockMod = playerCreature.GetSkillRank(Skill.OpenLock!);
        
        int result = roll + openLockMod;
        
        playerCreature.SpeakString(new SkillCheckString("Open Lock", roll, openLockMod, result).GetRollResult());
    }
}