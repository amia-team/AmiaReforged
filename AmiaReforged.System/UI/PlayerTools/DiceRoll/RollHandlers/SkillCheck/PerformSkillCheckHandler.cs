using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Perform)]
public class PerformSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int performMod = playerCreature.GetSkillRank(Skill.Perform!);
        
        int result = roll + performMod;
        
        string message = $"[?] Perform Skill Check = D20: {roll} + Perform Modifier ( {performMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}