using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

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
        
        string message = $"[?] Use Magic Device Skill Check = D20: {roll} + Use Magic Device Modifier ( {useMagicDeviceMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}