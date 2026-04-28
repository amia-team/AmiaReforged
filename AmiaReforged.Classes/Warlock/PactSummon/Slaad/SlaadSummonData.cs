using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactSummon.Slaad;

public static class SlaadSummonData
{
    public const string RedSlaad = "wlkslaadred";
    public const string BlueSlaad = "wlkslaadblue";
    public const string GreenSlaad = "wlkslaadgreen";
    public const string GraySlaad = "wlkslaadgray";

    public static readonly PactSummonBaseData SummonData = new()
    {
        Skills = [Skill.Discipline, Skill.Persuade],
        MovementRate = MovementRate.CreatureDefault,
        SharedEffect = Effect.LinkEffects(
            Effect.DamageResistance(DamageType.Acid, 5),
            Effect.DamageResistance(DamageType.Cold, 5),
            Effect.DamageResistance(DamageType.Electrical, 5),
            Effect.DamageResistance(DamageType.Fire, 5),
            Effect.DamageResistance(DamageType.Sonic, 5)
        )
    };

    public static PactSummonTierData GetTierData(string resRef, int tier) =>
        (resRef, tier) switch
        {
            (resRef: GraySlaad, tier: >= 7) => new PactSummonTierData
            {
                HitPoints = 210,
                ArmorBonus = 36,
                BaseAttackBonus = 22,
                BaseAttackCount = 3,
                SkillRank = 28,
                BaseSavingThrow = 28,
                Strength = 22,
                DamageBonus = DamageBonus.Plus2d12,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 8, interval: TimeSpan.FromSeconds(6)))
            },
            (resRef: GreenSlaad, tier: >= 6) => new PactSummonTierData
            {
                HitPoints = 180,
                ArmorBonus = 30,
                BaseAttackBonus = 19,
                BaseAttackCount = 3,
                SkillRank = 24,
                BaseSavingThrow = 24,
                Strength = 20,
                DamageBonus = DamageBonus.Plus2d10,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 6, interval: TimeSpan.FromSeconds(6)))
            },
            (resRef: GreenSlaad, _) => new PactSummonTierData
            {
                HitPoints = 150,
                ArmorBonus = 24,
                BaseAttackBonus = 16,
                BaseAttackCount = 2,
                SkillRank = 20,
                BaseSavingThrow = 20,
                Strength = 18,
                DamageBonus = DamageBonus.Plus2d8,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 6, interval: TimeSpan.FromSeconds(6)))
            },
            (resRef: BlueSlaad, tier: >= 4) => new PactSummonTierData
            {
                HitPoints = 120,
                ArmorBonus = 18,
                BaseAttackBonus = 13,
                BaseAttackCount = 2,
                SkillRank = 16,
                BaseSavingThrow = 16,
                Strength = 16,
                DamageBonus = DamageBonus.Plus2d6,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 4, interval: TimeSpan.FromSeconds(6)))
            },
            (resRef: BlueSlaad, _) => new PactSummonTierData
            {
                HitPoints = 90,
                ArmorBonus = 12,
                BaseAttackBonus = 10,
                BaseAttackCount = 1,
                SkillRank = 12,
                BaseSavingThrow = 12,
                Strength = 14,
                DamageBonus = DamageBonus.Plus1d6,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 4, interval: TimeSpan.FromSeconds(6)))
            },
            (resRef: RedSlaad, tier: >= 2) => new PactSummonTierData
            {
                HitPoints = 60,
                ArmorBonus = 6,
                BaseAttackBonus = 7,
                BaseAttackCount = 1,
                SkillRank = 8,
                BaseSavingThrow = 8,
                Strength = 12,
                DamageBonus = DamageBonus.Plus1d4,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 2, interval: TimeSpan.FromSeconds(6)))
            },
            _ => new PactSummonTierData
            {
                HitPoints = 30,
                ArmorBonus = 0,
                BaseAttackBonus = 4,
                BaseAttackCount = 1,
                SkillRank = 4,
                BaseSavingThrow = 4,
                Strength = 10,
                TierEffect = Effect.LinkEffects(Effect.Regenerate(amountPerInterval: 2, interval: TimeSpan.FromSeconds(6)))
            }
        };
}
