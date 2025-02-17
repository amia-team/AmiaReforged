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
        int fortitudeMod = playerCreature.GetSavingThrow(SavingThrow.Reflex);

        int result = roll + fortitudeMod;
        const string saveName = "Reflex";


        playerCreature.SpeakString(new SavingThrowString(saveName, roll, result).GetRollResult());
    }
}