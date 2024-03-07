using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Search)]
public class SearchSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int searchMod = playerCreature.GetSkillRank(Skill.Search!);
        
        int result = roll + searchMod;
        
        string message = $"<c � >[?] <c f�>Search Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Search Modifier ( <c�  >{searchMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}