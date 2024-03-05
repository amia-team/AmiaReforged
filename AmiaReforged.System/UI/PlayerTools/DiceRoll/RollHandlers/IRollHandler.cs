using Anvil.API;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

public interface IRollHandler
{
    public void RollDice(NwPlayer player);
}

public class DiceRollAttribute : Attribute
{
    public DiceRollType RollType { get; }

    public DiceRollAttribute(DiceRollType rollType)
    {
        RollType = rollType;
    }
}