using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfPersuade : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Persuade;

    protected override Skill Skill => Skill.Persuade;

    protected override string SkillName => "Persuade";
}
