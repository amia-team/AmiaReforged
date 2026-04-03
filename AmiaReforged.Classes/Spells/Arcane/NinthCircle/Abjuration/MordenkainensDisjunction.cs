using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.NinthCircle.Abjuration;

/// <summary>
/// Mordenkainen's Disjunction - An abjuration spell that attempts to strip magical effects.
///
/// Targeted: breaches 6 magical defences and attempts to dispel all spells on the target.
/// Area: breaches 2 magical defences and attempts to dispel 2 strongest spell on each creature.
///
/// Dispel check: 1d20 + caster level (max +10) + 2 per Abjuration Focus vs DC 12 + spell's caster level.
///
/// Spell resistance: no.
/// Creatures under Time Stop, petrification, or X1_L_IMMUNE_TO_DISPEL=10 are immune.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class MordenkainensDisjunction(DispelService dispelService, BreachService breachService) : ISpell
{
    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_MordDisj";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        int dispelModifier = dispelService.GetDispelModifier(caster, caster.CasterLevel, eventData.Spell);

        Effect impVfx = Effect.LinkEffects(Effect.VisualEffect(VfxType.ImpDispel),
            Effect.VisualEffect(VfxType.ImpBreach));
        Effect spellResistDecrease = Effect.SpellResistanceDecrease(10);
        spellResistDecrease.SubType = EffectSubType.Extraordinary;
        TimeSpan duration = NwTimeSpan.FromTurns(1);

        if (eventData.TargetObject is { } targetObject)
        {
            MordyTarget(caster, targetObject, dispelModifier, eventData.Spell, impVfx, spellResistDecrease, duration);
        }
        else if (eventData.TargetLocation is { } location)
        {
            MordyArea(caster, location, dispelModifier, impVfx, eventData.Spell, spellResistDecrease, duration);
        }
    }

    private void MordyTarget(NwCreature caster, NwGameObject target, int dispelModifier, NwSpell spell, Effect impVfx,
        Effect spellResistDecrease, TimeSpan duration)
    {
        Effect breachMagic = breachService.BreachMagic(breachAmount: 6, caster);
        dispelService.SignalDispel(caster, target, spell);

        target.ApplyEffect(EffectDuration.Instant, impVfx);
        target.ApplyEffect(EffectDuration.Instant, breachMagic);
        target.ApplyEffect(EffectDuration.Temporary, spellResistDecrease, duration);

        if (dispelService.IsImmuneToDispel(target)) return;

        Effect dispelMagic = dispelService.DispelMagic(dispelModifier, caster);
        target.ApplyEffect(EffectDuration.Instant, dispelMagic);

        breachService.FlushBreachFeedback(caster);
        dispelService.FlushDispelFeedback(caster);
    }

    private void MordyArea(NwCreature caster, Location location, int dispelModifier, Effect impVfx, NwSpell spell,
        Effect spellResistDecrease, TimeSpan duration)
    {
        Effect breachMagic = breachService.BreachMagic(breachAmount: 2, caster);
        Effect dispelMagic = dispelService.DispelMagic(dispelModifier, caster, maxSpells: 2);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfDispel));

        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, losCheck: true,
                     ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door | ObjectTypes.AreaOfEffect))
        {
            dispelService.SignalDispel(caster, targetObject, spell);

            if (targetObject is NwCreature creature && caster.IsReactionTypeFriendly(creature))
                continue;

            targetObject.ApplyEffect(EffectDuration.Instant, breachMagic);
            targetObject.ApplyEffect(EffectDuration.Instant, impVfx);
            targetObject.ApplyEffect(EffectDuration.Temporary, spellResistDecrease, duration);

            if (dispelService.IsImmuneToDispel(targetObject)) continue;

            targetObject.ApplyEffect(EffectDuration.Instant, dispelMagic);
        }

        breachService.FlushBreachFeedback(caster);
        dispelService.FlushDispelFeedback(caster);
    }
}
