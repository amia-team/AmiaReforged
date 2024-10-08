﻿using Anvil.API;
using NWN.Core;

// Keeps the color token more concise.
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D3)]
public class D3RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d3();

        playerCreature.SpeakString(new NumericDieString("D3", roll).GetRollResult());
    }
}