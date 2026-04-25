using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Bloodsworn.Boons.BoonOfSkill;

public abstract class SkillBoonBase : IBoon
{
    public abstract BoonType BoonType { get; }

    protected abstract Skill Skill { get; }

    protected abstract string SkillName { get; }

    public int GetBoonAmount(int bloodswornLevel)
        => bloodswornLevel * 3;

    public Effect GetBoonEffect(int bloodswornLevel)
        => Effect.SkillIncrease(Skill!, GetBoonAmount(bloodswornLevel));

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of {SkillName}: +{GetBoonAmount(bloodswornLevel)} {SkillName}";
}
