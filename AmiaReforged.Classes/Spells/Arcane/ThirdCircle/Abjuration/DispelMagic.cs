using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.ThirdCircle.Abjuration;

/// <summary>
/// Dispel Magic - An abjuration spell that attempts to strip magical effects.
///
/// Targeted: attempts to dispel all spells on the target.
/// Area: attempts to dispel 1 strongest spell on each creature.
///
/// Dispel check: 1d20 + caster level (max +10) + 2 per Abjuration Focus vs DC 12 + spell's caster level.
///
/// Spell resistance: no.
/// Creatures under Time Stop, petrification, or X1_L_IMMUNE_TO_DISPEL=10 are immune.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class DispelMagic(DispelService dispelService) : ISpell
{
    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_DisMagic";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        int dispelModifier = dispelService.GetDispelModifier(caster, caster.CasterLevel, eventData.Spell);
        Effect impVfx = Effect.VisualEffect(VfxType.ImpDispel);

        if (eventData.TargetObject is { } targetObject)
        {
            DispelTarget(caster, targetObject, dispelModifier, eventData.Spell, impVfx);
        }
        else if (eventData.TargetLocation is { } location)
        {
            DispelArea(caster, location, dispelModifier, impVfx, eventData.Spell);
        }
    }

    private void DispelTarget(NwCreature caster, NwGameObject target, int dispelModifier, NwSpell spell, Effect impVfx)
    {
        dispelService.SignalDispel(caster, target, spell);
        if (dispelService.IsImmuneToDispel(target)) return;

        Effect dispelMagic = dispelService.DispelMagic(dispelModifier, caster: caster);

        target.ApplyEffect(EffectDuration.Instant, dispelMagic);
        target.ApplyEffect(EffectDuration.Instant, impVfx);

        dispelService.FlushDispelFeedback(caster);
    }

    private void DispelArea(NwCreature caster, Location location, int dispelModifier, Effect impVfx, NwSpell spell)
    {
        Effect dispelMagic = dispelService.DispelMagic(dispelModifier, caster, maxSpells: 1);

        location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfDispel));

        foreach (NwGameObject targetObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, losCheck: true,
                     ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door | ObjectTypes.AreaOfEffect))
        {
            dispelService.SignalDispel(caster, targetObject, spell);

            if (targetObject is NwCreature creature && caster.IsReactionTypeFriendly(creature)
                || dispelService.IsImmuneToDispel(targetObject)) continue;

            targetObject.ApplyEffect(EffectDuration.Instant, dispelMagic);
            targetObject.ApplyEffect(EffectDuration.Instant, impVfx);
        }

        dispelService.FlushDispelFeedback(caster);
    }
}
