using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

public abstract class AbilityBoonBase : IBoon
{
    public abstract BoonType BoonType { get; }

    protected abstract Ability Ability { get; }

    public int GetBoonAmount(int bloodswornLevel) => bloodswornLevel switch
    {
        >= 5 => 3,
        >= 3 => 2,
        _ => 1
    };

    public Effect GetBoonEffect(int bloodswornLevel)
        => Effect.AbilityIncrease(Ability, GetBoonAmount(bloodswornLevel));

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of {nameof(Ability)}: +{GetBoonAmount(bloodswornLevel)} {nameof(Ability)}";
}
