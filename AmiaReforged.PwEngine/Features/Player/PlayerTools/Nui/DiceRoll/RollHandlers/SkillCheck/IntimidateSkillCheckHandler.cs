using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Intimidate)]
public class IntimidateSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int intimidateMod = playerCreature.GetSkillRank(Skill.Intimidate!);

        int result = roll + intimidateMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Intimidate", roll, intimidateMod, result)
            .GetRollResult());
    }
}
