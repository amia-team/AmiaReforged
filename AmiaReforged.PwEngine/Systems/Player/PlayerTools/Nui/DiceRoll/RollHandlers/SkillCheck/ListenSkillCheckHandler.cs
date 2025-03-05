using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Listen)]
public class ListenSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int listenMod = playerCreature.GetSkillRank(Skill.Listen!);

        int result = roll + listenMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Listen", roll, listenMod, result).GetRollResult());
    }
}