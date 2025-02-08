using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers;

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