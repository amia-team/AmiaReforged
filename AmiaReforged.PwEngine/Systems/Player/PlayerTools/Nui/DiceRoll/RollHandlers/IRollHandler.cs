using Anvil.API;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers;

public interface IRollHandler
{
    public void RollDice(NwPlayer player);
}

[MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
public class DiceRollAttribute : Attribute
{
    public DiceRollType RollType { get; }

    public DiceRollAttribute(DiceRollType rollType)
    {
        RollType = rollType;
    }
}