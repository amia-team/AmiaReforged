using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfPerform : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Perform;

    protected override Skill Skill => Skill.Perform;

    protected override string SkillName => "Perform";
}
