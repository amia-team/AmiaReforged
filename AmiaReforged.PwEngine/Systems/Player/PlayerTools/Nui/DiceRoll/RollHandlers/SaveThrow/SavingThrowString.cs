using Anvil.API;
using static AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SaveThrow;

public class SavingThrowString
{
    private readonly string _saveName;
    private readonly int _roll;
    private readonly int _result;
    private readonly int _modifier;

    public SavingThrowString(string saveName, int roll, int modifier, int result)
    {
        _saveName = saveName;
        _roll = roll;
        _result = result;
        _modifier = modifier;
    }

    public string GetRollResult() =>
        $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> {_saveName} Saving Throw = D20:</c> {_roll}<c{LightBlue.ToColorToken()}> + {_saveName} Modifier (</c><c{ColorConstants.Yellow.ToColorToken()}> {_modifier}</c><c{LightBlue.ToColorToken()}> ) = </c>{_result} <c{AmiaLime.ToColorToken()}>[?]</c>";
}