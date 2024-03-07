using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.CraftTrap)]
public class CraftTrapSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int craftTrapMod = playerCreature.GetSkillRank(Skill.CraftTrap!);
        
        int result = roll + craftTrapMod;
        
        string message = $"<c � >[?] <c f�>Craft Trap Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Craft Trap Modifier ( <c�  >{craftTrapMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}