using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterBluffSpot)]
public class CounterBluffSpotHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = player.LoginCreature.GetSkillRank(NwSkill.FromSkillType(Skill.Spot)!);

        string charSpot = $"<c þ >[?] </c><c fþ>Counter Bluff Spot Skill Check = D20:</c> <cþ  >{roll}</c>";
        string spotMod = $"<c þ > + Spot Modifier:</c> <cþ  >{modifier}</c>";

        if (player.LoginCreature == null) return;

        player.LoginCreature.SpeakString($"{charSpot} {spotMod}<c þ > = </c><cþ  >{roll + modifier}</c><c þ > [?]</c>");
    }
}