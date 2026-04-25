using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfAbility;

[ServiceBinding(typeof(IBoon))]
public class BoonOfWisdom : AbilityBoonBase
{
    public override BoonType BoonType => BoonType.Wisdom;

    protected override Ability Ability => Ability.Wisdom;
}
