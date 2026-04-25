using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfConcentration : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Concentration;

    protected override Skill Skill => Skill.Concentration;

    protected override string SkillName => "Concentration";
}
