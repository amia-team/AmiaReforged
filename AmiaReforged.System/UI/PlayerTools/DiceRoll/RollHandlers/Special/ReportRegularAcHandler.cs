using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportRegularAc)]
public class ReportRegularAcHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        playerCreature.SpeakString($"<c � >[?] My AC is: </c><c�  >{NWScript.GetAC(playerCreature)}</c><c � > [?]</c>");

    }
}