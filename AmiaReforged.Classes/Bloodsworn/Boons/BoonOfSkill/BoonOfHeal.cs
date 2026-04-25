using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfHeal : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Heal;

    protected override Skill Skill => Skill.Heal;

    protected override string SkillName => "Heal";
}
