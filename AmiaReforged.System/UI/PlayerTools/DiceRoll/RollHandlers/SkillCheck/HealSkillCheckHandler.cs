using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Heal)]
public class HealSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int healMod = playerCreature.GetSkillRank(Skill.Heal!);
        
        int result = roll + healMod;
        
        playerCreature.SpeakString(new SkillCheckString("Heal", roll, healMod, result).GetRollResult());
    }
}