using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportYourAlignment)]
public class ReportYourAlignmentHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        string alignment = playerCreature.LawChaosAlignment + " " + playerCreature.GoodEvilAlignment;
        string message = $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> My alignment is:</c> {alignment} <c{AmiaLime.ToColorToken()}>[?]</c>";
        
        playerCreature.SpeakString(message);
    }
}