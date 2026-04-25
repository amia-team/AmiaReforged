using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfUseMagicDevice : SkillBoonBase
{
    public override BoonType BoonType => BoonType.UseMagicDevice;

    protected override Skill Skill => Skill.UseMagicDevice;

    protected override string SkillName => "Use Magic Device";
}
