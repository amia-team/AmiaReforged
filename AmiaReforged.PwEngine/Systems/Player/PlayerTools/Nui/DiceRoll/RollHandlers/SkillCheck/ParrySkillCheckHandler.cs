using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Parry)]
public class ParrySkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int parryMod = playerCreature.GetSkillRank(Skill.Parry!);
        
        int result = roll + parryMod;
        
        playerCreature.SpeakString(new SkillCheckString("Parry", roll, parryMod, result).GetRollResult());
    }
}