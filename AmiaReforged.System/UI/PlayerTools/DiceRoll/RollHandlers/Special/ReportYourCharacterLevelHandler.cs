using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportYourCharacterLevel)]
public class ReportYourCharacterLevelHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        playerCreature.SpeakString($"<c � >[?] My character level is: </c><c�  >{playerCreature.Level}</c><c � > [?]</c>");
    }
}