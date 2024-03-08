using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportRegularAc)]
public class ReportRegularAcHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;

        playerCreature?.SpeakString($"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> My AC is:</c> {NWScript.GetAC(playerCreature)} <c{AmiaLime.ToColorToken()}>[?]</c>");

    }
}