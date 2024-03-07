using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Listen)]
public class ListenSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int listenMod = playerCreature.GetSkillRank(Skill.Listen!);
        
        int result = roll + listenMod;
        
        string message = $"<c � >[?] <c f�>Listen Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Listen Modifier ( <c�  >{listenMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}