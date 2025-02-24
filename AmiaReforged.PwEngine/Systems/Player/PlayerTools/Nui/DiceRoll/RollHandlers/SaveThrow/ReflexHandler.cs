using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SaveThrow;

[DiceRoll(DiceRollType.Fortitude)]
public class ReflexHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int saveMod = playerCreature.GetSavingThrow(SavingThrow.Reflex);

        int result = roll + saveMod;
        const string saveName = "Reflex";


        playerCreature.SpeakString(new SavingThrowString(saveName, roll, saveMod, result).GetRollResult());
    }
}