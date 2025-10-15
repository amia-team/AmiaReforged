using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Taunt)]
public class TauntSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int tauntMod = playerCreature.GetSkillRank(Skill.Taunt!);

        int result = roll + tauntMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Taunt", roll, tauntMod, result).GetRollResult());
    }
}
