using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.UseMagicDevice)]
public class UseMagicDeviceSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int useMagicDeviceMod = playerCreature.GetSkillRank(Skill.UseMagicDevice!);
        
        int result = roll + useMagicDeviceMod;
        
        playerCreature.SpeakString(new SkillCheckString("Use Magic Device", roll, useMagicDeviceMod, result).GetRollResult());
    }
}