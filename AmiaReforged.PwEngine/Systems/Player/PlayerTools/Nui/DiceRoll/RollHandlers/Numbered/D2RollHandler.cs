using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Numbered;

[DiceRoll(DiceRollType.D2)]
public class D2RollHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d2();
        playerCreature.SpeakString(new NumericDieString("D2", roll).GetRollResult());
    }
}