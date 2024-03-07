using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.RollTouchAttackStr)]
public class RollTouchAttackStrHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int diceRoll = NWScript.d20();
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        int sizeMod = (int)playerCreature.Size;
        int strMod = playerCreature.GetAbilityModifier(Ability.Strength);

        int result = diceRoll + baseAttackBonus + strMod + sizeMod;

        playerCreature.SpeakString(
            $"<c � >[?] <c f�>Strength Touch Attack</c> = D20: </c><c�  >{diceRoll}</c><c � > + Base Attack Bonus ( <c�  >{baseAttackBonus}</c><c � > ) + Strength Modifier ( <c�  >{strMod}</c><c � > ) + Size Modifier ( <c�  >{sizeMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>");
    }
}