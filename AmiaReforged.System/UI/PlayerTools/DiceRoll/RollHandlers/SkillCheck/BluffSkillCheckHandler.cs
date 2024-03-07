using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Bluff)]
public class BluffSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int bluffMod = playerCreature.GetSkillRank(Skill.Bluff!);
        
        int result = roll + bluffMod;
        
        string message = $"<c � >[?] <c f�>Bluff Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Bluff Modifier ( <c�  >{bluffMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}