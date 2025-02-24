using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Tumble)]
public class TumbleSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int tumbleMod = playerCreature.GetSkillRank(Skill.Tumble!);
        
        int result = roll + tumbleMod;
        
        playerCreature.SpeakString(new SkillCheckString("Tumble", roll, tumbleMod, result).GetRollResult());
    }
}