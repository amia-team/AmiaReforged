using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportTouchAttackWis)]
public class ReportTouchAttackWisHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int diceRoll = NWScript.d20();
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        int wisMod = playerCreature.GetAbilityModifier(Ability.Wisdom);
        int sizeMod = (int)playerCreature.Size;

        int result = diceRoll + baseAttackBonus + wisMod + sizeMod;

        playerCreature.SpeakString(
            $"<c \ufffd >[?] <c f\ufffd>Wisdom Touch Attack</c> = D20: </c><c\ufffd  >{diceRoll}</c><c \ufffd > + Base Attack Bonus ( <c\ufffd  >{baseAttackBonus}</c><c \ufffd > ) + Wisdom Modifier ( <c\ufffd  >{wisMod}</c><c \ufffd > ) + Size Modifier ( <c\ufffd  >{sizeMod}</c><c \ufffd > ) = <c\ufffd  >{result}</c><c \ufffd > [?]</c>");
    }
}