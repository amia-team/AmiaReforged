using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.SetTrap)]
public class SetTrapSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int setTrapMod = playerCreature.GetSkillRank(Skill.SetTrap!);
        
        int result = roll + setTrapMod;
        
        playerCreature.SpeakString(new SkillCheckString("Set Trap", roll, setTrapMod, result).GetRollResult());
    }
}