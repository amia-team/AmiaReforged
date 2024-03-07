using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Tumble)]
public class TumbleSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int tumbleMod = playerCreature.GetSkillRank(Skill.Tumble!);
        
        int result = roll + tumbleMod;
        
        string message = $"<c � >[?] <c f�>Tumble Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Tumble Modifier ( <c�  >{tumbleMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}