using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfMoveSilently : SkillBoonBase
{
    public override BoonType BoonType => BoonType.MoveSilently;

    protected override Skill Skill => Skill.MoveSilently;

    protected override string SkillName => "Move Silently";
}
