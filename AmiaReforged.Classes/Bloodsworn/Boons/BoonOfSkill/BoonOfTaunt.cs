using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfTaunt : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Taunt;

    protected override Skill Skill => Skill.Taunt;

    protected override string SkillName => "Taunt";
}
