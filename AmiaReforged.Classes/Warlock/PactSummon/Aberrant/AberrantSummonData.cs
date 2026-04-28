using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactSummon.Aberrant;

public static class AberrantSummonData
{
    public static readonly PactSummonBaseData SummonData = new()
    {
        ImmunityTypes = [ImmunityType.MindSpells],
        Skills = [Skill.Discipline],
        MovementRate = MovementRate.Slow,
        SharedEffect = Effect.DamageIncrease(bonus: 1, DamageType.Acid)
    };

    public static readonly Dictionary<int, PactSummonTierData> ByTier = new()
    {
        [7] = new PactSummonTierData
        {
            HitPoints = 150,
            ArmorBonus = 24,
            BaseAttackBonus = 22,
            BaseAttackCount = 1,
            SkillRank = 28,
            BaseSavingThrow = 28,
            Strength = 22,
            DamageBonus = DamageBonus.Plus2d12
        },
        [6] = new PactSummonTierData
        {
            HitPoints = 150,
            ArmorBonus = 24,
            BaseAttackBonus = 19,
            BaseAttackCount = 1,
            SkillRank = 24,
            BaseSavingThrow = 24,
            Strength = 20,
            DamageBonus = DamageBonus.Plus2d10
        },
        [5] = new PactSummonTierData
        {
            HitPoints = 120,
            ArmorBonus = 18,
            BaseAttackBonus = 16,
            BaseAttackCount = 1,
            SkillRank = 20,
            BaseSavingThrow = 20,
            Strength = 18,
            DamageBonus = DamageBonus.Plus2d8
        },
        [4] = new PactSummonTierData
        {
            HitPoints = 90,
            ArmorBonus = 12,
            BaseAttackBonus = 13,
            BaseAttackCount = 1,
            SkillRank = 16,
            BaseSavingThrow = 16,
            Strength = 16,
            DamageBonus = DamageBonus.Plus2d6
        },
        [3] = new PactSummonTierData
        {
            HitPoints = 90,
            ArmorBonus = 12,
            BaseAttackBonus = 10,
            BaseAttackCount = 1,
            SkillRank = 12,
            BaseSavingThrow = 12,
            Strength = 14,
            DamageBonus = DamageBonus.Plus1d6
        },
        [2] = new PactSummonTierData
        {
            HitPoints = 60,
            ArmorBonus = 6,
            BaseAttackBonus = 7,
            BaseAttackCount = 1,
            SkillRank = 8,
            BaseSavingThrow = 8,
            Strength = 12,
            DamageBonus = DamageBonus.Plus1d4
        },
        [1] = new PactSummonTierData
        {
            HitPoints = 30,
            ArmorBonus = 0,
            BaseAttackBonus = 4,
            BaseAttackCount = 1,
            SkillRank = 4,
            BaseSavingThrow = 4,
            Strength = 10
        }
    };
}
