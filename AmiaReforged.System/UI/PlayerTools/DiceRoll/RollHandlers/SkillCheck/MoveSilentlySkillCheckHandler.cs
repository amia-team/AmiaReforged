using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.MoveSilently)]
public class MoveSilentlySkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int moveSilentlyMod = playerCreature.GetSkillRank(Skill.MoveSilently!);
        
        int result = roll + moveSilentlyMod;
        
        playerCreature.SpeakString(new SkillCheckString("Move Silently", roll, moveSilentlyMod, result).GetRollResult());
    }
}