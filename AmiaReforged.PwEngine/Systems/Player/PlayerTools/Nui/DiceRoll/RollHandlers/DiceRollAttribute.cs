using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers;

[MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
public class DiceRollAttribute : Attribute
{
    public DiceRollAttribute(DiceRollType rollType)
    {
        RollType = rollType;
    }

    public DiceRollType RollType { get; }
}