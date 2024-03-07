using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Spot)]
public class SpotSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int spotMod = playerCreature.GetSkillRank(Skill.Spot!);
        
        int result = roll + spotMod;
        
        string message = $"<c � >[?] <c f�>Spot Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Spot Modifier ( <c�  >{spotMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}