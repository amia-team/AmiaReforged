using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterBluffListen)]
public class CounterBluffListenHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        player.SendServerMessage("You rolled a 20!");
    }
}