using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D3)]
public class D3RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d3();
        
        playerCreature.SpeakString($"<c � >[?] D3 Roll: </c><c�  >{roll}</c><c � > [?]</c>");
    }
}