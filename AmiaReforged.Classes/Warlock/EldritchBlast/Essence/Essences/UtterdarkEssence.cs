using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class UtterdarkEssence : IEssence
{
    public EssenceType Essence => EssenceType.Utterdark;

    public EssenceData GetEssenceData(int warlockLevel, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Negative,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Negative,
        DmgImpVfx: VfxType.ImpNegativeEnergy,
        BeamVfx: VfxType.BeamEvil,
        DoomVfx: WarlockVfx.FnfDoomNegative,
        PulseVfx: WarlockVfx.ImpPulseNegative,
        Effect: UtterdarkEffect,
        AllowStacking: true,
        Duration: EssenceDuration(warlockLevel),
        EffectImpVfx: VfxType.ImpReduceAbilityScore
    );

    private static Effect UtterdarkEffect => Effect.LinkEffects(Effect.VisualEffect(VfxType.DurCessateNegative),
        Effect.NegativeLevel(2));

    private static TimeSpan EssenceDuration(int warlockLevel)
    {
        int rounds = Math.Max(1, warlockLevel / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
