using Anvil.API;
using NWN.Core;
using static AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.CounterBluffSpot)]
public class CounterBluffSpotHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = player.LoginCreature.GetSkillRank(NwSkill.FromSkillType(Skill.Spot)!);

        string charSpot =
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Counter Bluff Spot Skill Check = D20:</c> {roll}<c{LightBlue.ToColorToken()}>";
        string spotMod =
            $" + Spot Modifier: ( </c><c{ColorConstants.Yellow.ToColorToken()}>{modifier}</c><c{LightBlue.ToColorToken()}> )";

        if (player.LoginCreature == null) return;

        player.LoginCreature.SpeakString(
            $"{charSpot} {spotMod} =</c> {roll + modifier} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}
