using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterBluffSpot)]
public class CounterBluffSpotHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        player.SendServerMessage("You rolled a 20!");
    }
}