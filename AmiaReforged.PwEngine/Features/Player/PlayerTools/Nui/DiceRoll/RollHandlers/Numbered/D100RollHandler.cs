using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D100)]
public class D100RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d100();

        playerCreature.SpeakString(new NumericDieString(rollType: "D100", roll).GetRollResult());
    }
}
