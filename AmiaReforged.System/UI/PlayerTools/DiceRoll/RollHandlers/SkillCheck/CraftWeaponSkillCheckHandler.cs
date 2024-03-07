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
        
        string message = $"<c � >[?] <c f�>Craft Weapon Skill Check</c> = D20: </c><c�  >{roll}</c><c � > + Craft Weapon Modifier ( <c�  >{craftWeaponMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}