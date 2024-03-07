using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Taunt)]
public class TauntSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int tauntMod = playerCreature.GetSkillRank(Skill.Taunt!);
        
        int result = roll + tauntMod;
        
        string message = $"<c � >[?] <c f�>Taunt Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Taunt Modifier ( <c�  >{tauntMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}