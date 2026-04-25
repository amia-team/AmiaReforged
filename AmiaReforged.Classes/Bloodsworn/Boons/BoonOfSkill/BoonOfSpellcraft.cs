using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

[ServiceBinding(typeof(IBoon))]
public class BoonOfSpellcraft : SkillBoonBase
{
    public override BoonType BoonType => BoonType.Spellcraft;

    protected override Skill Skill => Skill.Spellcraft;

    protected override string SkillName => "Spellcraft";
}
