using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

[ServiceBinding(typeof(IBoon))]
public class BoonOfShadows : IBoon
{
    public BoonType BoonType => BoonType.Shadows;

    public int GetBoonAmount(int bloodswornLevel) => bloodswornLevel * 2;

    public Effect GetBoonEffect(int bloodswornLevel)
    {
        int boonAmount = GetBoonAmount(bloodswornLevel);

        Effect effect = Effect.LinkEffects(
            Effect.SkillIncrease(Skill.MoveSilently!, boonAmount),
            Effect.SkillIncrease(Skill.Hide!, boonAmount),
            Effect.Ultravision()
        );

        return effect;
    }

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of Shadows: Ultravision, +{GetBoonAmount(bloodswornLevel)} Hide and Move Silently";
}
