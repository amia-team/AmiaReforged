using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Concentration)]
public class ConcentrationSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int concentrationMod = playerCreature.GetSkillRank(Skill.Concentration!);
        
        int result = roll + concentrationMod;
        
        string message = $"<c � >[?] <c f�>Concentration Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Concentration Modifier ( <c�  >{concentrationMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}