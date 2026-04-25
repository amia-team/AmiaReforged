using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfDiscipline : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Discipline;

    protected override Skill Skill => Skill.Discipline;

    protected override string SkillName => "Discipline";
}
