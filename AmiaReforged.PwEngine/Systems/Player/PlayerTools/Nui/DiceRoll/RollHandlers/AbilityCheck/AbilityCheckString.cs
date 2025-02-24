namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AbilityCheck;
using static AmiaColors;
using static Anvil.API.ColorConstants;

public class AbilityCheckString
{
    private readonly string _abilityName;
    private readonly int _roll;
    private readonly int _abilityModifier;
    
    public AbilityCheckString(string abilityName, int roll, int abilityModifier)
    {
        _abilityName = abilityName;
        _roll = roll;
        _abilityModifier = abilityModifier;
    }
    
    public string GetAbilityCheckString()
    {
        return $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> {_abilityName} Check = D20:</c> {_roll} <c{LightBlue.ToColorToken()}>+ {_abilityName} Modifier ( </c><c{Yellow.ToColorToken()}>{_abilityModifier}</c><c{LightBlue.ToColorToken()}> ) = </c>{_roll + _abilityModifier} <c{AmiaLime.ToColorToken()}>[?]</c>";
    }
}