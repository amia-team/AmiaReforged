using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

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

        playerCreature.SpeakString(
            new SkillCheckString(skillName: "Open Lock", roll, openLockMod, result).GetRollResult());
    }
}
