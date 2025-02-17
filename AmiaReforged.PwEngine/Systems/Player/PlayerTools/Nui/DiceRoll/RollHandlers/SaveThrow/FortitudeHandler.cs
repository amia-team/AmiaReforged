using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SaveThrow;

[DiceRoll(DiceRollType.Fortitude)]
public class FortitudeHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int fortitudeMod = playerCreature.GetSavingThrow(SavingThrow.Fortitude);

        int result = roll + fortitudeMod;
        const string saveName = "Fortitude";


        playerCreature.SpeakString(new SavingThrowString(saveName, roll, result).GetRollResult());
    }
}