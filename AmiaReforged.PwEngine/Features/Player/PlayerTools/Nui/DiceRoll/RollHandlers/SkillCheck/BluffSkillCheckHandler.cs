using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Bluff)]
public class BluffSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int bluffMod = playerCreature.GetSkillRank(Skill.Bluff!);

        int result = roll + bluffMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Bluff", roll, bluffMod, result).GetRollResult());
    }
}
