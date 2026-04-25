using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

[ServiceBinding(typeof(IBoon))]
public class BoonOfIntelligence : AbilityBoonBase
{
    public override BoonType BoonType => BoonType.Intelligence;

    protected override Ability Ability => Ability.Intelligence;
}
