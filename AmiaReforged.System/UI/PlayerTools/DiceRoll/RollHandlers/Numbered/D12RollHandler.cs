using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D12)]
public class D12RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d12();
        
        playerCreature.SpeakString($"<c � >[?] D12 Roll: </c><c�  >{roll}</c><c � > [?]</c>");
    }
}