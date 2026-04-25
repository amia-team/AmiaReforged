using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfOpenLock : SkillBoonBase
{
    public override BoonType BoonType => BoonType.OpenLock;

    protected override Skill Skill => Skill.OpenLock;

    protected override string SkillName => "Open Lock";
}
