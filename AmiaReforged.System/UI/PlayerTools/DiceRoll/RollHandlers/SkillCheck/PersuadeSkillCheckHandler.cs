using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Persuade)]
public class PersuadeSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int persuadeMod = playerCreature.GetSkillRank(Skill.Persuade!);
        
        int result = roll + persuadeMod;
        
        string message = $"<c � >[?] <c f�>Persuade Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Persuade Modifier ( <c�  >{persuadeMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}