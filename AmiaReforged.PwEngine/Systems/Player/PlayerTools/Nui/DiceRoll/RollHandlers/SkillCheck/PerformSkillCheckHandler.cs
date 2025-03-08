using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Perform)]
public class PerformSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int performMod = playerCreature.GetSkillRank(Skill.Perform!);

        int result = roll + performMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Perform", roll, performMod, result)
            .GetRollResult());
    }
}