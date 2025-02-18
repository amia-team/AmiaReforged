namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Numbered;

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
            $"<c{AmiaLime.ToColorToken()}>[?] </c><c{LightBlue.ToColorToken()}>{_rollType} =</c> {_rollResult} <c{AmiaLime.ToColorToken()}> [?]</c>";
    }
}