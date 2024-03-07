using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Special;

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
            $"[?] Grapple Check = D20: {diceRoll} + Base Attack Bonus ( {baseAttackBonus} ) + Strength Modifier ( {strMod} ) + Size Modifier ( {sizeMod} ) = {result} [?]";

        playerCreature.SpeakString(grapple);
    }
}