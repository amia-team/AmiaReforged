using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class HorizikaulsBoom : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X2_S0_HoriBoom";

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster is not NwCreature casterCreature) return;

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        SpellUtils.SignalSpell(casterCreature, target, eventData.Spell);

        if (ResistedSpell) return;

        int numberOfDice = Math.Min(casterCreature.CasterLevel / 2, 15);
        if (numberOfDice < 1) numberOfDice = 1;
        int totalDamage = SpellUtils.MaximizeSpell(eventData.MetaMagicFeat, 4, numberOfDice);
        totalDamage = SpellUtils.EmpowerSpell(eventData.MetaMagicFeat, totalDamage);

        int dc = SpellUtils.GetSpellDc(eventData);

        bool hasEpicFocus = casterCreature.KnowsFeat(Feat.EpicSpellFocusEvocation!);

        ApplyBoomDamage(casterCreature, target, totalDamage, dc, hasEpicFocus);

        // Epic Spell Focus Evocation: sonic pulse AoE around the target
        if (!hasEpicFocus) return;
        if (target.Location is not { } targetLocation) return;

        Effect pulseVfx = Effect.VisualEffect(VfxType.ImpPulseCold);
        target.ApplyEffect(EffectDuration.Instant, pulseVfx);

        foreach (NwCreature nearby in targetLocation.GetObjectsInShapeByType<NwCreature>(
                     Shape.Sphere, RadiusSize.Medium, true))
        {
            if (nearby == target) continue;
            if (!SpellUtils.IsValidHostileTarget(nearby, casterCreature)) continue;

            ApplyBoomDamage(casterCreature, nearby, totalDamage, dc, true);
        }
    }

    private static void ApplyBoomDamage(NwCreature caster, NwGameObject target, int totalDamage, int dc,
        bool applyDaze)
    {
        int adjustedDamage =
            NWScript.GetReflexAdjustedDamage(totalDamage, target, dc, NWScript.SAVING_THROW_TYPE_SONIC, caster);

        if (adjustedDamage <= 0) return;

        bool failedSave = adjustedDamage >= totalDamage;

        // Split damage: half sonic, half bludgeoning
        int sonicDamage = adjustedDamage / 2;
        int bludgeoningDamage = adjustedDamage - sonicDamage;

        Effect sonicDmg = Effect.Damage(sonicDamage, DamageType.Sonic);
        Effect bludgeoningDmg = Effect.Damage(bludgeoningDamage, DamageType.Bludgeoning);
        Effect vfx = Effect.VisualEffect(VfxType.ImpSonic);

        target.ApplyEffect(EffectDuration.Instant, sonicDmg);
        target.ApplyEffect(EffectDuration.Instant, bludgeoningDmg);
        target.ApplyEffect(EffectDuration.Instant, vfx);

        // Epic Evocation: daze on failed save
        if (!applyDaze || !failedSave || target is not NwCreature creature) return;

        int dazeRounds = Random.Shared.Roll(4, 1);
        Effect daze = Effect.LinkEffects(Effect.Dazed(), Effect.VisualEffect(VfxType.DurMindAffectingNegative));
        creature.ApplyEffect(EffectDuration.Temporary, daze, NwTimeSpan.FromRounds(dazeRounds));
    }
}
