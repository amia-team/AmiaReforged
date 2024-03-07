﻿using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.RollGrappleCheck)]
public class RollGrappleCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature playerCreature = player.LoginCreature;
        if (playerCreature == null) return;

        int diceRoll = NWScript.d20();
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        int strMod = playerCreature.GetAbilityModifier(Ability.Strength);
        int sizeMod = (int)playerCreature.Size;

        int result = diceRoll + baseAttackBonus + strMod + sizeMod;

        string grapple =
            $"<c � >[?] <c f�>Grapple Check</c> = D20: </c><c�  >{diceRoll}</c><c � > + Base Attack Bonus ( <c�  >{baseAttackBonus}</c><c � > ) + Strength Modifier ( <c�  >{strMod}</c><c � > ) + Size Modifier ( <c�  >{sizeMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";

        playerCreature.SpeakString(grapple);
    }
}