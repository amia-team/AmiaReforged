using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class HellrimeEssence : IEssence
{
    public EssenceType Essence => EssenceType.Hellrime;

    public EssenceData GetEssenceData(int warlockLevel, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Cold,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Cold,
        DmgImpVfx: VfxType.ImpFrostS,
        BeamVfx: VfxType.BeamCold,
        DoomVfx: WarlockVfx.FnfDoomCold,
        PulseVfx: WarlockVfx.ImpPulseCold,
        Effect: HellrimeEffect,
        Duration: EssenceDuration(warlockLevel)
    );

    private static Effect HellrimeEffect => Effect.LinkEffects(Effect.AbilityDecrease(Ability.Dexterity, 4),
            Effect.VisualEffect(VfxType.DurIceskin));

    private static TimeSpan EssenceDuration(int warlockLevel)
    {
        int rounds = Math.Max(1, warlockLevel / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
