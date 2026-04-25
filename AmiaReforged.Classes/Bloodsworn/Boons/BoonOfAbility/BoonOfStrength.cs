using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

[ServiceBinding(typeof(IBoon))]
public class BoonOfStrength : AbilityBoonBase
{
    public override BoonType BoonType => BoonType.Strength;

    protected override Ability Ability => Ability.Strength;
}
