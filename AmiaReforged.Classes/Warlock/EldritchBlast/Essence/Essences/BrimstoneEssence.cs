using AmiaReforged.Classes.EffectUtils.DamageOverTime;
using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class BrimstoneEssence(DamageOverTimeService dotService) : IEssence
{
    public EssenceType Essence => EssenceType.Brimstone;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Fire,
        SavingThrow: SavingThrow.Reflex,
        SavingThrowType: SavingThrowType.Fire,
        DmgImpVfx: VfxType.ImpFlameS,
        BeamVfx: VfxType.BeamFire,
        DoomVfx: WarlockVfx.FnfDoomFire,
        PulseVfx: WarlockVfx.ImpPulseFire,
        Effect: BrimstoneEffect(warlock),
        Duration: EssenceDuration(invocationCl)
    );

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }

    private Effect BrimstoneEffect(NwCreature warlock) => dotService.DotEffect(warlock, 6, 2, DamageType.Fire, VfxType.DurInfernoChest, VfxType.ImpFlameS);
}
