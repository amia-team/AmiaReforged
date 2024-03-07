using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportYourCharacterLevel)]
public class ReportYourCharacterLevelHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        playerCreature.SpeakString($"[?] My character level is: {playerCreature.Level} [?]");
    }
}