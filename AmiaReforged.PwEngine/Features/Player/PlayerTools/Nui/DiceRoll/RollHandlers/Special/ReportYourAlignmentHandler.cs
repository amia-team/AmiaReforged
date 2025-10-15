using Anvil.API;
using static AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.ReportYourAlignment)]
public class ReportYourAlignmentHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        string alignment = playerCreature.LawChaosAlignment + " " + playerCreature.GoodEvilAlignment;
        string message =
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> My alignment is:</c> {alignment} <c{AmiaLime.ToColorToken()}>[?]</c>";

        playerCreature.SpeakString(message);
    }
}
