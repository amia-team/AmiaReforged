using Anvil.API;
using NWN.Core;
using static AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;
using static Anvil.API.ColorConstants;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

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
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Strength Touch Attack = D20: </c>{diceRoll}<c{LightBlue.ToColorToken()}> + Base Attack Bonus (</c> <c{Yellow.ToColorToken()}>{baseAttackBonus}</c><c{LightBlue.ToColorToken()}> ) + Strength Modifier (</c> <c{Yellow.ToColorToken()}>{strMod}</c><c{LightBlue.ToColorToken()}> ) + Size Modifier (</c> <c{Yellow.ToColorToken()}>{sizeMod}</c><c{LightBlue.ToColorToken()}> ) =</c> {result} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}
