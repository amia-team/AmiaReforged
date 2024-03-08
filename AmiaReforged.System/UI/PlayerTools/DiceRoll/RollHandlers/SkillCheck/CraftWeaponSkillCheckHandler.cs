using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.CraftWeapon)]
public class CraftWeaponSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int craftWeaponMod = playerCreature.GetSkillRank(Skill.CraftWeapon!);
        
        int result = roll + craftWeaponMod;
        
        
        playerCreature.SpeakString(new SkillCheckString("Craft Weapon", roll, craftWeaponMod, result).GetRollResult());
    }
}