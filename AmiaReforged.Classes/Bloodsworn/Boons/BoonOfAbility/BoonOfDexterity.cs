using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

[ServiceBinding(typeof(IBoon))]
public class BoonOfDexterity : AbilityBoonBase
{
    public override BoonType BoonType => BoonType.Dexterity;

    protected override Ability Ability => Ability.Dexterity;
}
