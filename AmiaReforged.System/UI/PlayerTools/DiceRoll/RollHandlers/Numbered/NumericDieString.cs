using Anvil.Services;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Numbered;

using static AmiaColors;

public class NumericDieString
{
    private readonly int _rollResult;
    private readonly string _rollType;


    public NumericDieString(string rollType, int rollResult)
    {
        _rollType = rollType;
        _rollResult = rollResult;
    }

    public string GetRollResult()
    {
        return
            $"<c{LimeGreen.ToColorToken()}>[?] </c><c{LightBlue.ToColorToken()}>{_rollType} =</c> {_rollResult} <c{LimeGreen.ToColorToken()}> [?]</c>";
    }
}