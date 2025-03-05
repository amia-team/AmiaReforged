using Anvil.API;

namespace AmiaReforged.Classes.Warlock.Types;

// Constants are defined here for ease of use when calling in service handlers.

public static class WarlockSummonConstants
{
    /// <summary>
    ///     immune to mind-affecting spells, +1 damage effect acid (bypasses resistance, visual flair)
    /// </summary>
    public readonly static Effect AberrationEffects = Effect.LinkEffects(Effect.DamageIncrease(1, DamageType.Acid), Effect.Immunity(ImmunityType.MindSpells));
    
    /// <summary>
    ///     immune to sneak attacks, critical hits, mind-affecting spells, paralysis, poison, and disease, death magic, level drain, and ability drain
    /// </summary>
    public readonly static Effect UndeadImmunities = Effect.LinkEffects(Effect.Immunity(ImmunityType.SneakAttack), Effect.Immunity(ImmunityType.CriticalHit), 
        Effect.Immunity(ImmunityType.MindSpells), Effect.Immunity(ImmunityType.Paralysis), Effect.Immunity(ImmunityType.Poison), Effect.Immunity(ImmunityType.Disease),
        Effect.Immunity(ImmunityType.Death), Effect.Immunity(ImmunityType.NegativeLevel), Effect.Immunity(ImmunityType.AbilityDecrease));

    public static Effect CelestialEffects(int concealment)
    {
        return Effect.LinkEffects(Effect.VisualEffect(VfxType.DurGhostSmoke2),  Effect.VisualEffect(VfxType.DurLightWhite20), Effect.Concealment(concealment),
            UndeadImmunities);
    }
    public static Effect FeyEffects(int concealment)
    {
        return Effect.LinkEffects(Effect.VisualEffect(VfxType.DurAuraGreenLight), Effect.VisualEffect(VfxType.DurInvisibility), Effect.Concealment(concealment),
            UndeadImmunities);
    } 
        
    public static Effect SlaadEffects(int regen)
    {
        return Effect.LinkEffects(Effect.DamageResistance(DamageType.Acid, 5), Effect.DamageResistance(DamageType.Cold, 5), Effect.DamageResistance(DamageType.Electrical, 5), 
        Effect.DamageResistance(DamageType.Fire, 5), Effect.DamageResistance(DamageType.Sonic, 5), Effect.Regenerate(regen, TimeSpan.FromSeconds(6)));
    }
}