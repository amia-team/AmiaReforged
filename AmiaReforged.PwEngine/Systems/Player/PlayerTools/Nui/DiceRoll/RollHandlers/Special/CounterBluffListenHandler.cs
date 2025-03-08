using Anvil.API;
using NWN.Core;
using static AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.CounterBluffListen)]
public class CounterBluffListenHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = player.LoginCreature.GetSkillRank(NwSkill.FromSkillType(Skill.Listen)!);

        string charListen =
            $"<c{AmiaLime.ToColorToken()}>[?]</c> <c{LightBlue.ToColorToken()}>Counter Bluff Listen Skill Check = D20: </c>{roll}<c{LightBlue.ToColorToken()}>";
        string listenMod =
            $" + Listen Modifier (</c> <c{ColorConstants.Yellow.ToColorToken()}>{modifier}</c> <c{LightBlue.ToColorToken()}>)";

        if (player.LoginCreature == null) return;

        player.LoginCreature.SpeakString(
            $"{charListen} {listenMod} =</c> {roll + modifier} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}