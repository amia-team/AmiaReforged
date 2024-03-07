using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.CraftArmor)]
public class CraftArmorSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int craftArmorMod = playerCreature.GetSkillRank(Skill.CraftArmor!);
        
        int result = roll + craftArmorMod;
        
        string message = $"[?] Craft Armor Skill Check = D20: {roll} + Craft Armor Modifier ( {craftArmorMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}