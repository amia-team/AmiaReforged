using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class BewitchingEssence : IEssence
{
    public EssenceType Essence => EssenceType.Bewitching;

    public EssenceData GetEssenceData(int warlockLevel) => new
    (
        Type: Essence,
        DamageType: DamageType.Magical,
        SavingThrow: SavingThrow.Will,
        SavingThrowType: SavingThrowType.MindSpells,
        DmgImpVfx: VfxType.ImpMagblue,
        BeamVfx: VfxType.BeamMind,
        DoomVfx: WarlockVfx.FnfDoomMind,
        PulseVfx: WarlockVfx.ImpPulseMind,
        Effect: BewitchingEffect,
        EffectImpVfx: VfxType.ImpCharm,
        Duration: EssenceDuration(warlockLevel)
    );

    private static Effect BewitchingEffect =>
        Effect.LinkEffects(Effect.Confused(), Effect.VisualEffect(VfxType.DurMindAffectingDisabled));

    private static TimeSpan EssenceDuration(int warlockLevel)
    {
        int rounds = Math.Max(1, warlockLevel / 10);
        return NwTimeSpan.FromRounds(rounds);
    }
}
