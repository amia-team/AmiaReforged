using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfDisableTrap : SkillBoonBase
{
    public override BoonType BoonType => BoonType.DisableTrap;

    protected override Skill Skill => Skill.DisableTrap;

    protected override string SkillName => "Disable Trap";
}
