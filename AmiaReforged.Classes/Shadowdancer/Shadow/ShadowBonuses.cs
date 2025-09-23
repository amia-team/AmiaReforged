using Anvil.API;

namespace AmiaReforged.Classes.Shadowdancer.Shadow;

public static class ShadowBonuses
{
    private static readonly Dictionary<int, (int Dodge, int Save, int Slashing, int Bludgeoning)> BonusMap = new()
    {
        { 14, (1, 1, 0, 0) },
        { 15, (1, 1, 10, 10) },
        { 16, (2, 2, 10, 10) },
        { 17, (2, 2, 20, 20) },
        { 18, (3, 3, 20, 20) },
        { 19, (3, 3, 30, 30) },
        { 20, (6, 6, 50, 50) }
    };

    public static void ApplyShadowBonuses(NwCreature shadowDancer, NwCreature shadow)
    {
        byte sdLevel = shadowDancer.GetClassInfo(ClassType.Shadowdancer)?.Level ?? 0;

        if (!BonusMap.TryGetValue(sdLevel, out (int Dodge, int Save, int Slashing, int Bludgeoning) shadowBonuses))
            return;

        Effect shadowBonusEffect = Effect.LinkEffects
        (
            Effect.ACIncrease(shadowBonuses.Dodge),
            Effect.SavingThrowIncrease(SavingThrow.All, shadowBonuses.Save),
            Effect.DamageImmunityIncrease(DamageType.Slashing, shadowBonuses.Slashing),
            Effect.DamageImmunityIncrease(DamageType.Bludgeoning, shadowBonuses.Bludgeoning)
        );
        shadowBonusEffect.SubType = EffectSubType.Unyielding;

        shadow.ApplyEffect(EffectDuration.Permanent, shadowBonusEffect);

        shadow.GetObjectVariable<LocalVariableInt>("sd_level").Value = sdLevel;
    }
}
