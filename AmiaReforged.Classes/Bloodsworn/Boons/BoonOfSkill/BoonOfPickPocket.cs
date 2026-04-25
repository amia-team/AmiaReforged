using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfPickPocket : SkillBoonBase
{
    public override BoonType BoonType => BoonType.PickPocket;

    protected override Skill Skill => Skill.PickPocket;

    protected override string SkillName => "Pick Pocket";
}
