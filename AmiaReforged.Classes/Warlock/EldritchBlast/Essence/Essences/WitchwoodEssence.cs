using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class WitchwoodEssence : IEssence
{
    public EssenceType Essence => EssenceType.Witchwood;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Magical,
        SavingThrow: SavingThrow.Reflex,
        SavingThrowType: SavingThrowType.Spell,
        DmgImpVfx: VfxType.ImpBigbysForcefulHand,
        BeamVfx: VfxType.BeamDisintegrate,
        DoomVfx: AmiaVfxTypes.FnfDoomNature,
        PulseVfx: AmiaVfxTypes.ImpPulseEarthChest,
        HideousBlowVfx: AmiaItemVisuals.GreenHue,
        Effect: EntangleEffect,
        Duration: EssenceDuration(invocationCl)
    );

    private static Effect EntangleEffect
        => Effect.LinkEffects(Effect.Entangle(), Effect.VisualEffect(VfxType.DurEntangle));

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 10);
        return NwTimeSpan.FromRounds(rounds);
    }
}
