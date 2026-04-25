using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfListen : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Listen;

    protected override Skill Skill => Skill.Listen;

    protected override string SkillName => "Listen";
}
