using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;
using static Anvil.API.ColorConstants;

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
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Wisdom Touch Attack = D20: </c>{diceRoll}<c{LightBlue.ToColorToken()}> + Base Attack Bonus ( </c><c{Yellow.ToColorToken()}>{baseAttackBonus}</c><c{LightBlue.ToColorToken()}> ) + Wisdom Modifier (</c> <c{Yellow.ToColorToken()}>{wisMod}</c><c{LightBlue.ToColorToken()}> ) + Size Modifier (</c> <c{Yellow.ToColorToken()}>{sizeMod}</c><c{LightBlue.ToColorToken()}> ) = </c>{result} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}