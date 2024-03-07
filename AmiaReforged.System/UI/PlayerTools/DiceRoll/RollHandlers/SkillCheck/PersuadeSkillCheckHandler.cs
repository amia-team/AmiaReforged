using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Persuade)]
public class PersuadeSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int persuadeMod = playerCreature.GetSkillRank(Skill.Persuade!);
        
        int result = roll + persuadeMod;
        
        string message = $"[?] Persuade Skill Check = D20: {roll} + Persuade Modifier ( {persuadeMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}