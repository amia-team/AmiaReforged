﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Search)]
public class SearchSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int searchMod = playerCreature.GetSkillRank(Skill.Search!);

        int result = roll + searchMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Search", roll, searchMod, result).GetRollResult());
    }
}