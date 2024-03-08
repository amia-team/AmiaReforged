using Anvil.API;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportYourCharacterLevel)]
public class ReportYourCharacterLevelHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;

        playerCreature?.SpeakString(
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> My character level is:</c> {playerCreature.Level} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}