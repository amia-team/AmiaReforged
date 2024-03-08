using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;
using static Anvil.API.ColorConstants;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterIntimidate)]
public class CounterIntimidateHandler : IRollHandler
{
    /// 
    /// Counter Intimidate using 3.0 rules
    /// 
    /// 
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = NWScript.GetHitDice(player.LoginCreature);
        int wisMod = NWScript.GetAbilityModifier(NWScript.ABILITY_WISDOM, player.LoginCreature);

        string charIntimidate =
            $"<c{AmiaColors.AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Counter Intimidate Skill Check = D20: </c>{roll}<c{LightBlue.ToColorToken()}>";
        string characterLevel = $" + Character Level: ( </c><c{Yellow.ToColorToken()}>{modifier}</c><c{LightBlue.ToColorToken()}> )";
        string wisdomMod = $" + Wisdom Modifier ( </c><c{Yellow.ToColorToken()}>{wisMod}</c><c{LightBlue.ToColorToken()}> )";

        if (player.LoginCreature == null) return;

        player.LoginCreature.SpeakString(
            $"{charIntimidate} {characterLevel} {wisdomMod} =</c> {roll + modifier + wisMod} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}