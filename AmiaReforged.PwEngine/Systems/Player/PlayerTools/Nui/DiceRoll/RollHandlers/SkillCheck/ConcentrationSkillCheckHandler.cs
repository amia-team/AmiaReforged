using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Concentration)]
public class ConcentrationSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int concentrationMod = playerCreature.GetSkillRank(Skill.Concentration!);

        int result = roll + concentrationMod;


        playerCreature.SpeakString(new SkillCheckString(skillName: "Concentration", roll, concentrationMod, result)
            .GetRollResult());
    }
}