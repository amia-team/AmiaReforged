using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class ScreamingEssence : IEssence
{
    public EssenceType Essence => EssenceType.Screaming;

    public EssenceData GetEssenceData(int warlockLevel) => new
    (
        Type: Essence,
        DamageType: DamageType.Sonic,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Sonic,
        DmgImpVfx: VfxType.ImpSonic,
        BeamVfx: VfxType.BeamSilentCold,
        DoomVfx: WarlockVfx.FnfDoomSonic,
        PulseVfx: WarlockVfx.ImpPulseWind,
        Effect: ScreamingEffect,
        EffectImpVfx: VfxType.ImpBlindDeafM,
        Duration: EssenceDuration(warlockLevel)
    );

    private static Effect ScreamingEffect => Effect.LinkEffects(Effect.VisualEffect(VfxType.DurCessateNegative),
        Effect.DamageImmunityDecrease(DamageType.Sonic, 15));

    private static TimeSpan EssenceDuration(int warlockLevel)
    {
        int rounds = Math.Max(1, warlockLevel / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
