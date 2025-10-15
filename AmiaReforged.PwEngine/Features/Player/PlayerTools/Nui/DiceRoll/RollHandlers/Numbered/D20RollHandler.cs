using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D20)]
public class D20RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();

        playerCreature.SpeakString(new NumericDieString(rollType: "D20", roll).GetRollResult());
    }
}
