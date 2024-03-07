using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D8)]
public class D8RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d8();
        
        playerCreature.SpeakString($"<c � >[?] D8 Roll: </c><c�  >{roll}</c><c � > [?]</c>");
    }
}