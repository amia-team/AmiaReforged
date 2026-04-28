using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactSummon.Fey;

public static class FeySummonData
{
    private const VfxType DurPartyDust = (VfxType)2563;

    public static readonly PactSummonBaseData SummonData = new()
    {
        ImmunityTypes = [ImmunityType.CriticalHit, ImmunityType.SneakAttack, ImmunityType.MindSpells,
            ImmunityType.Disease, ImmunityType.Paralysis, ImmunityType.Poison, ImmunityType.AbilityDecrease,
            ImmunityType.Death, ImmunityType.NegativeLevel],
        Skills = [Skill.AnimalEmpathy, Skill.Discipline, Skill.Perform],
        MovementRate = MovementRate.CreatureDefault,
        SharedEffect = Effect.LinkEffects(Effect.VisualEffect(DurPartyDust),
            Effect.VisualEffect(VfxType.DurGlowGreen))
    };

    public static readonly Dictionary<int, PactSummonTierData> ByTier = new()
    {
        [7] = new PactSummonTierData
        {
            HitPoints = 210,
            ArmorBonus = 36,
            BaseAttackBonus = 22,
            BaseAttackCount = 3,
            SkillRank = 28,
            BaseSavingThrow = 28,
            Strength = 22,
            DamageBonus = DamageBonus.Plus2d12,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 50))
        },
        [6] = new PactSummonTierData
        {
            HitPoints = 180,
            ArmorBonus = 30,
            BaseAttackBonus = 19,
            BaseAttackCount = 3,
            SkillRank = 24,
            BaseSavingThrow = 24,
            Strength = 20,
            DamageBonus = DamageBonus.Plus2d10,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 45))
        },
        [5] = new PactSummonTierData
        {
            HitPoints = 150,
            ArmorBonus = 24,
            BaseAttackBonus = 16,
            BaseAttackCount = 2,
            SkillRank = 20,
            BaseSavingThrow = 20,
            Strength = 18,
            DamageBonus = DamageBonus.Plus2d8,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 40))
        },
        [4] = new PactSummonTierData
        {
            HitPoints = 120,
            ArmorBonus = 18,
            BaseAttackBonus = 13,
            BaseAttackCount = 2,
            SkillRank = 16,
            BaseSavingThrow = 16,
            Strength = 16,
            DamageBonus = DamageBonus.Plus2d6,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 35))
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
            DamageBonus = DamageBonus.Plus1d6,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 30))
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
            DamageBonus = DamageBonus.Plus1d4,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 25))
        }
        ,[1] = new PactSummonTierData
        {
            HitPoints = 30,
            ArmorBonus = 0,
            BaseAttackBonus = 4,
            BaseAttackCount = 1,
            SkillRank = 4,
            BaseSavingThrow = 4,
            Strength = 10,
            TierEffect = Effect.LinkEffects(Effect.Concealment(percentage: 20))
        }
    };
}
