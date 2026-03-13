using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class DrainingEssence : IEssence
{
    public EssenceType Essence => EssenceType.Draining;

    public EssenceData GetEssenceData(int warlockLevel, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Magical,
        SavingThrow: SavingThrow.Will,
        SavingThrowType: SavingThrowType.Spell,
        DmgImpVfx: VfxType.ImpMagblue,
        BeamVfx: VfxType.BeamOdd,
        DoomVfx: WarlockVfx.FnfDoomOdd,
        PulseVfx: WarlockVfx.ImpPulseOdd,
        Effect: DrainingEffect,
        EffectImpVfx: VfxType.ImpSlow,
        Duration: EssenceDuration(warlockLevel)
    );

    private static Effect DrainingEffect
        => Effect.LinkEffects(Effect.Slow(), Effect.VisualEffect(VfxType.DurCessateNegative));

    private static TimeSpan EssenceDuration(int warlockLevel)
    {
        int rounds = Math.Max(1, warlockLevel / 10);
        return NwTimeSpan.FromRounds(rounds);
    }
}
