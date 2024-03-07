using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Intimidate)]
public class IntimidateSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int intimidateMod = playerCreature.GetSkillRank(Skill.Intimidate!);
        
        int result = roll + intimidateMod;
        
        string message = $"<c � >[?] <c f�>Intimidate Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Intimidate Modifier ( <c�  >{intimidateMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}