using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.CraftTrap)]
public class CraftTrapSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int craftTrapMod = playerCreature.GetSkillRank(Skill.CraftTrap!);
        
        int result = roll + craftTrapMod;
        
        playerCreature.SpeakString(new SkillCheckString("Craft Trap", roll, craftTrapMod, result).GetRollResult());
    }
}