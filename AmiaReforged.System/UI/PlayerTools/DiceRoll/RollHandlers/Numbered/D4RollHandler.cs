using Anvil.API;
using NWN.Core;

// Keeps the color token more concise.
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D4)]
public class D4RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d4();

        playerCreature.SpeakString(
            new NumericDieString("D4", roll).GetRollResult());
    }
}