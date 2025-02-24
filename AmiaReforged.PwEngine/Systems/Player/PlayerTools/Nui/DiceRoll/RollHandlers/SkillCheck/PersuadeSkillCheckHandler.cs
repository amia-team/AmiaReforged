using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

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
        
        playerCreature.SpeakString(new SkillCheckString("Persuade", roll, persuadeMod, result).GetRollResult());
    }
}