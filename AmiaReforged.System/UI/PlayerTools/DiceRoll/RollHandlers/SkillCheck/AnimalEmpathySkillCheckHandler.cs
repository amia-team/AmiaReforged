using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.AnimalEmpathy)]
public class AnimalEmpathySkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int appraiseMod = playerCreature.GetSkillRank(Skill.AnimalEmpathy!);
        
        int result = roll + appraiseMod;
        
        string message = $"[?] Animal Empathy Skill Check = D20: {roll} + Animal Empathy Modifier ( {appraiseMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}