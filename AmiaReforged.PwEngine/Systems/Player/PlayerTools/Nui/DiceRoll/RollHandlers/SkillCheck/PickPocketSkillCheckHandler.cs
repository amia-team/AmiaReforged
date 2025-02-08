using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.PickPocket)]
public class PickPocketSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int pickPocketMod = playerCreature.GetSkillRank(Skill.PickPocket!);
        
        int result = roll + pickPocketMod;
        
        playerCreature.SpeakString(new SkillCheckString("Pick Pocket", roll, pickPocketMod, result).GetRollResult());
    }
}