using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.RollTouchAttackDex)]
public class RollTouchAttackDexHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int dexMod = playerCreature.GetAbilityModifier(Ability.Dexterity);
        int sizeMod = (int)playerCreature.Size;
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        
        int result = roll + dexMod + sizeMod + baseAttackBonus;
        
        string message = $"<c � >[?] <c f�>Dexterity Touch Attack</c> = D20: </c><c�  >{roll}</c><c � > + Dexterity Modifier ( <c�  >{dexMod}</c><c � > ) + Size Modifier ( <c�  >{sizeMod}</c><c � > ) + Base Attack Bonus ( <c�  >{baseAttackBonus}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}