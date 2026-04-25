using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfParry : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Parry;

    protected override Skill Skill => Skill.Parry;

    protected override string SkillName => "Parry";
}
