using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Lore)]
public class LoreSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int loreMod = playerCreature.GetSkillRank(Skill.Lore!);
        
        int result = roll + loreMod;
        
        string message = $"[?] Lore Skill Check = D20: {roll} + Lore Modifier ( {loreMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}