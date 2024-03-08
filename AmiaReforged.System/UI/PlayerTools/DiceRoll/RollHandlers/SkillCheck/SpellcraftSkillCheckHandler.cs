using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Spellcraft)]
public class SpellcraftSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int spellcraftMod = playerCreature.GetSkillRank(Skill.Spellcraft!);
        
        int result = roll + spellcraftMod;
        
        playerCreature.SpeakString(new SkillCheckString("Spellcraft", roll, spellcraftMod, result).GetRollResult());
    }
}