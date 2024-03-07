using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D4)]
public class D4RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d4();
        
        playerCreature.SpeakString($"<c � >[?] D4 Roll: </c><c�  >{roll}</c><c � > [?]</c>");
    }
}