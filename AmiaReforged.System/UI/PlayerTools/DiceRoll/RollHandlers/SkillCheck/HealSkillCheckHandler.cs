using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Heal)]
public class HealSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int healMod = playerCreature.GetSkillRank(Skill.Heal!);
        
        int result = roll + healMod;
        
        string message = $"<c � >[?] <c f�>Heal Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Heal Modifier ( <c�  >{healMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}