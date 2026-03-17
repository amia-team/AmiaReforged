using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class FrightfulEssence : IEssence
{
    public EssenceType Essence => EssenceType.Frightful;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Magical,
        SavingThrow: SavingThrow.Will,
        SavingThrowType: SavingThrowType.Fear,
        DmgImpVfx: VfxType.ImpMagblue,
        BeamVfx: VfxType.BeamMind,
        DoomVfx: WarlockVfx.FnfDoomMind,
        PulseVfx: WarlockVfx.ImpPulseMind,
        Effect: FrightfulEffect,
        Duration: EssenceDuration(invocationCl)
    );

    private static Effect FrightfulEffect =>
        Effect.LinkEffects(Effect.Frightened(), Effect.VisualEffect(VfxType.DurMindAffectingFear));

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 10);
        return NwTimeSpan.FromRounds(rounds);
    }
}
