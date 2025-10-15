using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.DisableTrap)]
public class DisableTrapSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int disableTrapMod = playerCreature.GetSkillRank(Skill.DisableTrap!);

        int result = roll + disableTrapMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Disable Trap", roll, disableTrapMod, result)
            .GetRollResult());
    }
}
