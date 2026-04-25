using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfSpot : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Spot;

    protected override Skill Skill => Skill.Spot;

    protected override string SkillName => "Spot";
}
