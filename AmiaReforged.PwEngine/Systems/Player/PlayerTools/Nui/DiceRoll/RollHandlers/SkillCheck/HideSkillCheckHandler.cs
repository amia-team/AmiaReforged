using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Hide)]
public class HideSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int hideMod = playerCreature.GetSkillRank(Skill.Hide!);
        
        int result = roll + hideMod;
        
        playerCreature.SpeakString(new SkillCheckString("Hide", roll, hideMod, result).GetRollResult());
    }
}