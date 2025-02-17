using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SaveThrow;

[DiceRoll(DiceRollType.Will)]
public class WillHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int fortitudeMod = playerCreature.GetSavingThrow(SavingThrow.Will);

        int result = roll + fortitudeMod;
        const string saveName = "Will";


        playerCreature.SpeakString(new SavingThrowString(saveName, roll, result).GetRollResult());
    }
}