using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportTouchAttackWis)]
public class ReportTouchAttackWisHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int diceRoll = NWScript.d20();
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        int wisMod = playerCreature.GetAbilityModifier(Ability.Wisdom);
        int sizeMod = (int)playerCreature.Size;

        int result = diceRoll + baseAttackBonus + wisMod + sizeMod;

        playerCreature.SpeakString(
            $"[?] Wisdom Touch Attack = D20: {diceRoll} + Base Attack Bonus ( {baseAttackBonus} ) + Wisdom Modifier ( {wisMod} ) + Size Modifier ( {sizeMod} ) = {result} [?]");
    }
}