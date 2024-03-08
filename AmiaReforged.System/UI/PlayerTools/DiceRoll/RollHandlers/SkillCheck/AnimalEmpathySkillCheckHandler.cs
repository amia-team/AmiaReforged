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

        playerCreature.SpeakString(new SkillCheckString("Animal Empathy", roll, appraiseMod, result).GetRollResult());
    }
}