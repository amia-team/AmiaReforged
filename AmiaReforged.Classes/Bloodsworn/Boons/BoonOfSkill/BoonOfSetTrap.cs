using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfSetTrap : SkillBoonBase
{
    public override BoonType BoonType => BoonType.SetTrap;

    protected override Skill Skill => Skill.SetTrap;

    protected override string SkillName => "Set Trap";
}
