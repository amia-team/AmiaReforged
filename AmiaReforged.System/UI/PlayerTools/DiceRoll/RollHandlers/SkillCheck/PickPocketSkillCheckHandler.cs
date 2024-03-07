using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.PickPocket)]
public class PickPocketSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int pickPocketMod = playerCreature.GetSkillRank(Skill.PickPocket!);
        
        int result = roll + pickPocketMod;
        
        string message = $"<c � >[?] <c f�>Pick Pocket Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Pick Pocket Modifier ( <c�  >{pickPocketMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}