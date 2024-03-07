using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.SetTrap)]
public class SetTrapSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int setTrapMod = playerCreature.GetSkillRank(Skill.SetTrap!);
        
        int result = roll + setTrapMod;
        
        string message = $"<c � >[?] <c f�>Set Trap Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Set Trap Modifier ( <c�  >{setTrapMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}