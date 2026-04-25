using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfHide : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Hide;

    protected override Skill Skill => Skill.Hide;

    protected override string SkillName => "Hide";
}
