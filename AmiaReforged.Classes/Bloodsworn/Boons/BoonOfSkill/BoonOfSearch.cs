using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfSearch : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Search;

    protected override Skill Skill => Skill.Search;

    protected override string SkillName => "Search";
}
