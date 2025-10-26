using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic.EffectLists;

[ServiceBinding(typeof(AdverseWildMagic))]
public class AdverseWildMagic(WildMagicUtils wildMagicUtils)
{
    public void Polymorph(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        monk.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.GetRandomPolymorph(), WildMagicUtils.LongDuration);

    public void InternalConfusion(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        Effect confused = Effect.Confused();
        confused.SubType = EffectSubType.Magical;
        confused.IgnoreImmunity = true;

        monk.ApplyEffect(EffectDuration.Temporary, confused, WildMagicUtils.ShortDuration);
    }

    public void TradePlaces(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        if (monk.Location == null || target.Location == null) return;

        monk.ActionJumpToLocation(target.Location);
        target.ActionJumpToLocation(monk.Location);

        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagblue));
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagblue));
    }

    public void HealNotThatOne(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        target.ApplyEffect(EffectDuration.Instant, Effect.Heal(100));
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingG));
    }

    public void RestorationNotThatOne(NwCreature monk, NwCreature target, int dc, byte monkLevel)
    {
        target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpRestoration));

        foreach (Effect effect in target.ActiveEffects)
        {
            if (effect.EffectType is EffectType.AbilityDecrease or EffectType.AcDecrease or EffectType.DamageDecrease
                or EffectType.DamageImmunityDecrease or EffectType.SavingThrowDecrease or EffectType.SkillDecrease
                or EffectType.Blindness or EffectType.Deaf or EffectType.Paralyze or EffectType.NegativeLevel
                && effect.SubType != EffectSubType.Unyielding)

                target.RemoveEffect(effect);
        }
    }

    public void SelfImmolation(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        monk.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.CombustEffect(monk, monk, monkLevel), WildMagicUtils.LongDuration);


    public void DeathArmorNotThatOne(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        target.ApplyEffect(EffectDuration.Temporary, wildMagicUtils.DeathArmorEffect(monk, monkLevel),  WildMagicUtils.LongDuration);

    public void SelfInflictWounds(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        monk.ApplyEffect(EffectDuration.Instant, wildMagicUtils.InflictLightWoundsEffect(monk, monkLevel));

    public void Stasis(NwCreature monk, NwCreature target, int dc, byte monkLevel) =>
        monk.ApplyEffect(EffectDuration.Temporary, Effect.CutsceneParalyze(), WildMagicUtils.ShortDuration);
}
