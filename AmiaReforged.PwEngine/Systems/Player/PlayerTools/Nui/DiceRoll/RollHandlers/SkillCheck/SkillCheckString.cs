using Anvil.API;
using static AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

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
        $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> {_skillName} Skill Check = D20:</c> {_roll}<c{LightBlue.ToColorToken()}> + {_skillName} Modifier (</c><c{ColorConstants.Yellow.ToColorToken()}> {_modifier}</c><c{LightBlue.ToColorToken()}> ) = </c>{_result} <c{AmiaLime.ToColorToken()}>[?]</c>";
}