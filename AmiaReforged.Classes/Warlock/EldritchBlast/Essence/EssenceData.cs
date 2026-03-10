using Anvil.API;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence;

public readonly record struct EssenceData
(
    EssenceType Type,
    DamageType DamageType,
    SavingThrow SavingThrow,
    SavingThrowType SavingThrowType,
    VfxType DmgImpVfx,
    VfxType BeamVfx,
    VfxType DoomVfx,
    VfxType PulseVfx,
    bool AllowStacking = false,
    bool BypassSpellResistance = false,
    Effect? Effect = null,
    VfxType? EffectImpVfx = null,
    TimeSpan? Duration = null
);
