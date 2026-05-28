using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class StormEssence : IEssence
{
    public EssenceType Essence => EssenceType.Storm;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Electrical,
        SavingThrow: SavingThrow.Reflex,
        SavingThrowType: SavingThrowType.Electricity,
        DmgImpVfx: VfxType.ImpLightningS,
        BeamVfx: VfxType.BeamLightning,
        DoomVfx: AmiaVfxTypes.FnfDoomElectric,
        PulseVfx: AmiaVfxTypes.ImpPulseAirChest,
        HideousBlowVfx: ItemVisual.Electrical,
        Effect: StormEffect(invocationCl)
    );

    private static Effect StormEffect(int invocationCl) => Effect.Damage(DamageRoll(invocationCl), DamageType.Electrical);

    private static int DamageRoll(int invocationCl) => Random.Shared.Roll(4, invocationCl / 5);
}
