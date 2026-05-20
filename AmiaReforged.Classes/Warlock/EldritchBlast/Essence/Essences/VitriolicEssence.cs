using AmiaReforged.Classes.EffectUtils.DamageOverTime;
using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class VitriolicEssence(DamageOverTimeService dotService) : IEssence
{
    public EssenceType Essence => EssenceType.Vitriolic;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Acid,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Acid,
        DmgImpVfx: VfxType.ImpAcidS,
        BeamVfx: VfxType.BeamDisintegrate,
        DoomVfx: WarlockVfx.FnfDoomAcid,
        PulseVfx: WarlockVfx.ImpPulseNature,
        Effect: VitriolicEffect(warlock),
        AllowStacking: true,
        Duration: EssenceDuration(invocationCl)
    );

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }

    private Effect VitriolicEffect(NwCreature warlock) => dotService.DotEffect(warlock, 6, 2, DamageType.Acid, VfxType.DurAuraGreenDark, VfxType.ImpAcidS);
}
