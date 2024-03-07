using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Perform)]
public class PerformSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int performMod = playerCreature.GetSkillRank(Skill.Perform!);
        
        int result = roll + performMod;
        
        string message = $"<c � >[?] <c f�>Perform Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Perform Modifier ( <c�  >{performMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}