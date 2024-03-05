using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportYourCharacterLevel)]
public class ReportYourCharacterLevelHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        player.SendServerMessage("You rolled a 20!");
    }
}