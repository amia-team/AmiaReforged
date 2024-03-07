using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Taunt)]
public class TauntSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int tauntMod = playerCreature.GetSkillRank(Skill.Taunt!);
        
        int result = roll + tauntMod;
        
        string message = $"[?] Taunt Skill Check = D20: {roll} + Taunt Modifier ( {tauntMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}