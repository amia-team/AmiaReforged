using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.DisableTrap)]
public class DisableTrapSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int disableTrapMod = playerCreature.GetSkillRank(Skill.DisableTrap!);
        
        int result = roll + disableTrapMod;
        
        string message = $"<c � >[?] <c f�>Disable Trap Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Disable Trap Modifier ( <c�  >{disableTrapMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}