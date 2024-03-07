using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Discipline)]
public class DisciplineSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int disciplineMod = playerCreature.GetSkillRank(Skill.Discipline!);
        
        int result = roll + disciplineMod;
        
        string message = $"[?] Discipline Skill Check = D20: {roll} + Discipline Modifier ( {disciplineMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}