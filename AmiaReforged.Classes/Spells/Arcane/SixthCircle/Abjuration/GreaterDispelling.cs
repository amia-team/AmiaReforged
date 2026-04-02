using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SixthCircle.Abjuration;

/// <summary>
/// Greater Dispelling - A powerful abjuration spell that removes magical effects.
///
/// Targeted: dispels all spells on the target.
/// Area: dispels the best (highest CL) spells on each creature.
///
/// Dispel check: 1d20 + caster level (max +15) + 2 per Abjuration Focus vs. DC 12 + spell effect's caster level
///
/// Spell resistance: no.
/// Creatures under Time Stop, petrification, or X1_L_IMMUNE_TO_DISPEL=10 are immune.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class GreaterDispelling(DispelService dispelService) : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_GrDispel";

    public void SetSpellResisted(bool result) => ResistedSpell = result;

    // Greater Dispelling bypasses spell resistance
    public void DoSpellResist(NwCreature creature, NwCreature caster) { }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        int dispelModifier = dispelService.GetDispelModifier(caster, caster.CasterLevel, eventData.Spell);
        Effect breachVfx = Effect.VisualEffect(VfxType.ImpBreach);

        if (eventData.TargetObject is { } targetObject)
        {
            DispelTarget(caster, targetObject, dispelModifier, eventData.Spell, breachVfx);
        }
        else if (eventData.TargetLocation is { } location)
        {
            DispelArea(caster, location, dispelModifier, breachVfx, eventData.Spell);
        }
    }

    private void DispelTarget(NwCreature caster, NwGameObject target, int dispelModifier, NwSpell spell, Effect breachVfx)
    {
        dispelService.SignalDispel(caster, target, spell);

        if (dispelService.IsImmuneToDispel(target)) return;

        dispelService.DispelTarget(caster, target, dispelModifier);
        target.ApplyEffect(EffectDuration.Instant, breachVfx);
    }

    private void DispelArea(NwCreature caster, Location location, int dispelModifier, Effect breachVfx, NwSpell spell)
    {
        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfDispelGreater));

        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, losCheck: true,
                     ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door | ObjectTypes.AreaOfEffect))
        {
            if (targetObject is NwAreaOfEffect { Spell: not null } aoeObject)
            {
                if (dispelService.TryDispelAreaOfEffect(caster, aoeObject, dispelModifier))
                    aoeObject.ApplyEffect(EffectDuration.Instant, breachVfx);
                continue;
            }

            dispelService.SignalDispel(caster, targetObject, spell);

            if (dispelService.IsImmuneToDispel(targetObject)) continue;

            dispelService.DispelTarget(caster, targetObject, dispelModifier, maxSpells: 1);
            targetObject.ApplyEffect(EffectDuration.Instant, breachVfx);
        }
    }
}
