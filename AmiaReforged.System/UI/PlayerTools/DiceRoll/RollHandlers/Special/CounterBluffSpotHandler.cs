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

        string charSpot = $"[?] Counter Bluff Spot Skill Check = D20: {roll}";
        string spotMod = $" + Spot Modifier: {modifier}";

        if (player.LoginCreature == null) return;

        player.LoginCreature.SpeakString($"{charSpot} {spotMod} = {roll + modifier} [?]");
    }
}