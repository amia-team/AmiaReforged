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
            $"[?] Strength Touch Attack = D20: {diceRoll} + Base Attack Bonus ( {baseAttackBonus} ) + Strength Modifier ( {strMod} ) + Size Modifier ( {sizeMod} ) = {result} [?]");
    }
}