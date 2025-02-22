using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.BuffRemover;

public static class EffectWhitelist
{
    public static IReadOnlyList<EffectType> Whitelist => new List<EffectType>
    {
        EffectType.TrueSeeing,
        EffectType.Ultravision,
        EffectType.SeeInvisible,
        EffectType.AcIncrease,
        EffectType.AttackIncrease,
        EffectType.AbilityIncrease,
        EffectType.DamageReduction,
        EffectType.DamageResistance,
        EffectType.DamageIncrease,
        EffectType.Immunity,
        EffectType.Invisibility,
        EffectType.Haste,
        EffectType.Ethereal,
        EffectType.Ultravision,
        EffectType.ElementalShield,
        EffectType.Polymorph
    };
}