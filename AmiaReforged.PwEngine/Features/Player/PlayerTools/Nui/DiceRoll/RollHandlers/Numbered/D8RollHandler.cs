// Keeps the color token more concise.

using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D8)]
public class D8RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d8();

        playerCreature.SpeakString(
            new NumericDieString(rollType: "D8", roll).GetRollResult());
    }
}
