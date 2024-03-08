using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Appraise)]
public class AppraiseSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int appraiseMod = playerCreature.GetSkillRank(Skill.Appraise!);
        
        int result = roll + appraiseMod;
        
        playerCreature.SpeakString(new SkillCheckString("Appraise", roll, appraiseMod, result).GetRollResult());
    }
}