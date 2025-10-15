using Anvil.API;
using NWN.Core;
using static AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;
using static Anvil.API.ColorConstants;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.RollGrappleCheck)]
public class RollGrappleCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature playerCreature = player.LoginCreature;
        if (playerCreature == null) return;

        int diceRoll = NWScript.d20();
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        int strMod = playerCreature.GetAbilityModifier(Ability.Strength);
        int sizeMod = (int)playerCreature.Size;

        int result = diceRoll + baseAttackBonus + strMod + sizeMod;

        string grapple =
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Grapple Check = D20: </c>{diceRoll}<c{LightBlue.ToColorToken()}> + Base Attack Bonus ( </c><c{Yellow.ToColorToken()}>{baseAttackBonus}</c> <c{LightBlue.ToColorToken()}>) + Strength Modifier ( </c><c{Yellow.ToColorToken()}>{strMod}</c><c{LightBlue.ToColorToken()}> ) + Size Modifier ( </c><c{Yellow.ToColorToken()}>{sizeMod}</c><c{LightBlue.ToColorToken()}> ) = </c>{result} <c{AmiaLime.ToColorToken()}>[?]</c>";

        playerCreature.SpeakString(grapple);
    }
}
