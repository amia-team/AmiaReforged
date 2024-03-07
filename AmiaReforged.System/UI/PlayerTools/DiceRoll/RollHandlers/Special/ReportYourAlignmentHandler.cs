using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportYourAlignment)]
public class ReportYourAlignmentHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        string alignment = playerCreature.LawChaosAlignment + " " + playerCreature.GoodEvilAlignment;
        string message = $"[?] My alignment is: {alignment} [?]";
        
        playerCreature.SpeakString(message);
    }
}