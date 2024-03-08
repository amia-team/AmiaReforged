using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

public class SkillCheckString
{
    private string _skillName;
    private int _roll;
    private int _modifier;
    private int _result;

    public SkillCheckString(string skillName, int roll, int modifier, int result)
    {
        _skillName = skillName;
        _roll = roll;
        _modifier = modifier;
        _result = result;
    }

    public string GetRollResult() =>
        $"<c{LimeGreen.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> {_skillName} Skill Check = D20:</c> {_roll}<c{LightBlue.ToColorToken()}> + {_skillName} Modifier ( {_modifier} ) = </c>{_result} <c{LimeGreen.ToColorToken()}>[?]</c>";
}