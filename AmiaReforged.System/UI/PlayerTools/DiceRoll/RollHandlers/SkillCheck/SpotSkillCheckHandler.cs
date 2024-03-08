using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Spot)]
public class SpotSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int spotMod = playerCreature.GetSkillRank(Skill.Spot!);
        
        int result = roll + spotMod;
        
        playerCreature.SpeakString(new SkillCheckString("Spot", roll, spotMod, result).GetRollResult());
    }
}