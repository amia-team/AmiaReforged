using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

[ServiceBinding(typeof(IBoon))]
public class BoonOfPerception : IBoon
{
    public BoonType BoonType => BoonType.Perception;

    public int GetBoonAmount(int bloodswornLevel) => bloodswornLevel * 2;

    public Effect GetBoonEffect(int bloodswornLevel)
    {
        int boonAmount = GetBoonAmount(bloodswornLevel);

        Effect effect = Effect.LinkEffects(
            Effect.SkillIncrease(Skill.Spot!, boonAmount),
            Effect.SkillIncrease(Skill.Listen!, boonAmount),
            Effect.SeeInvisible()
        );

        return effect;
    }

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of Perception: See Invisibility, +{GetBoonAmount(bloodswornLevel)} Listen and Spot";
}
