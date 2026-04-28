using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactSummon;

public readonly record struct PactSummonBaseData
(
    Skill[] Skills,
    MovementRate MovementRate = MovementRate.CreatureDefault,
    CreatureSize Size = CreatureSize.Medium,
    ImmunityType[]? ImmunityTypes = null,
    Effect? SharedEffect = null
);

public readonly record struct PactSummonTierData
(
    int HitPoints,
    sbyte ArmorBonus,
    int BaseAttackBonus,
    int BaseAttackCount,
    sbyte SkillRank,
    sbyte BaseSavingThrow,
    byte Strength,
    DamageBonus? DamageBonus,
    Effect? TierEffect = null
);
