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
        
        string message = $"[?] Move Silently Skill Check = D20: {roll} + Move Silently Modifier ( {moveSilentlyMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}