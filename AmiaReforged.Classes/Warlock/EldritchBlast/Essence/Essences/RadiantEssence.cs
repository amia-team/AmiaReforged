using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class RadiantEssence : IEssence
{
    public EssenceType Essence => EssenceType.Radiant;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Positive,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Positive,
        DmgImpVfx: VfxType.ImpSunstrike,
        BeamVfx: VfxType.BeamHoly,
        DoomVfx: AmiaVfxTypes.FnfDoomHoly,
        PulseVfx: AmiaVfxTypes.ImpPulseHolyChest,
        HideousBlowVfx: ItemVisual.Holy,
        Effect: RadiantEffect,
        Duration: EssenceDuration(invocationCl)
    );

    private static Effect RadiantEffect => Effect.LinkEffects
    (
        Effect.VisualEffect(VfxType.DurCessateNegative),
        Effect.EnemyAttackBonus(2),
        Effect.VisualEffect(AmiaVfxTypes.DurOrbYellow),
        Effect.VisualEffect(VfxType.DurLightYellow10)
    );

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
