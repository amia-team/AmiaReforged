﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Charisma)]
public class CharismaCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int chaMod = playerCreature.GetAbilityModifier(Ability.Charisma);

        playerCreature.SpeakString(
            new AbilityCheckString(abilityName: "Charisma", roll, chaMod).GetAbilityCheckString());
    }
}