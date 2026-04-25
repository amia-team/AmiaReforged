using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

[ServiceBinding(typeof(IBoon))]
public class BoonOfCharisma : AbilityBoonBase
{
    public override BoonType BoonType => BoonType.Charisma;

    protected override Ability Ability => Ability.Charisma;
}
