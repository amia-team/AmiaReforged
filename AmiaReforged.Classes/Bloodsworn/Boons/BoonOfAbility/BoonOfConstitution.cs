using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

[ServiceBinding(typeof(IBoon))]
public class BoonOfConstitution : AbilityBoonBase
{
    public override BoonType BoonType => BoonType.Constitution;

    protected override Ability Ability => Ability.Constitution;
}
