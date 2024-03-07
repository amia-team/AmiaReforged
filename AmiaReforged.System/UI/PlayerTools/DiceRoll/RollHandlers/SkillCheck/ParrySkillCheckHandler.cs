using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Parry)]
public class ParrySkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int parryMod = playerCreature.GetSkillRank(Skill.Parry!);
        
        int result = roll + parryMod;
        
        string message = $"<c � >[?] <c f�>Parry Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Parry Modifier ( <c�  >{parryMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}