using Anvil.API;
using NWN.Core;
using static AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;
using static Anvil.API.ColorConstants;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

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

        string message =
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Dexterity Touch Attack = D20: </c>{roll}<c{LightBlue.ToColorToken()}> + Dexterity Modifier ( </c><c{Yellow.ToColorToken()}>{dexMod}</c><c{LightBlue.ToColorToken()}> ) + Size Modifier (</c> <c{Yellow.ToColorToken()}>{sizeMod}</c><c{LightBlue.ToColorToken()}> ) + Base Attack Bonus (</c> <c{Yellow.ToColorToken()}>{baseAttackBonus}</c><c{LightBlue.ToColorToken()}> ) = </c>{result} <c{AmiaLime.ToColorToken()}>[?]</c>";

        playerCreature.SpeakString(message);
    }
}
