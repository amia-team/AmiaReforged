using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.OpenLock)]
public class OpenLockSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int openLockMod = playerCreature.GetSkillRank(Skill.OpenLock!);
        
        int result = roll + openLockMod;
        
        string message = $"<c � >[?] <c f�>Open Lock Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Open Lock Modifier ( <c�  >{openLockMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}