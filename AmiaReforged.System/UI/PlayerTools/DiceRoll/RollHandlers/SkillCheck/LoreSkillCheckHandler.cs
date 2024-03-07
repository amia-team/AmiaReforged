using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Lore)]
public class LoreSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int loreMod = playerCreature.GetSkillRank(Skill.Lore!);
        
        int result = roll + loreMod;
        
        string message = $"<c � >[?] <c f�>Lore Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Lore Modifier ( <c�  >{loreMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}