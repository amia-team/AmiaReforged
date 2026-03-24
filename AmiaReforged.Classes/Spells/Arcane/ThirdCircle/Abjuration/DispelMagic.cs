using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Arcane.ThirdCircle.Abjuration;

/// <summary>
/// Dispel Magic - An abjuration spell that attempts to strip magical effects.
///
/// Targeted: attempts to dispel all effects on the target.
/// Area: attempts to dispel the most powerful effect on each creature.
///
/// Dispel check: 1d20 + caster level (max +10) vs DC 11 + spell effect's caster level.
/// Abjuration focus bonuses (non-cumulative, applied to all casters):
///   Spell Focus +1, Greater +3, Epic +6.
/// Maximum effective caster level: 30.
///
/// Spell resistance: no.
/// Creatures under Time Stop, petrification, or X1_L_IMMUNE_TO_DISPEL=10 are immune.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class DispelMagic(DispelService dispelService) : ISpell
{
    private const int BaseCasterLevelCap = 10;
    private const int MaxCasterLevel = 30;

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_DisMagic";

    public void SetSpellResisted(bool result) => ResistedSpell = result;

    /// <summary>Dispel Magic bypasses spell resistance entirely.</summary>
    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        CheckedSpellResistance = true;
        ResistedSpell = false;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        int casterLevel = CalculateEffectiveCasterLevel(caster);
        Effect breachVfx = Effect.VisualEffect(VfxType.ImpBreach);
        Effect impactVfx = Effect.VisualEffect(VfxType.FnfDispel);

        if (eventData.TargetObject is { } targetObject)
        {
            DoTargetedDispel(caster, targetObject, casterLevel, breachVfx, impactVfx, eventData.Spell);
        }
        else if (eventData.TargetLocation is { } targetLocation)
        {
            DoAreaDispel(caster, targetLocation, casterLevel, breachVfx, impactVfx, eventData.Spell);
        }
    }

    private static int CalculateEffectiveCasterLevel(NwCreature caster)
    {
        int casterLevel = Math.Min(caster.CasterLevel, BaseCasterLevelCap);

        int bonus = caster switch
        {
            _ when caster.KnowsFeat(Feat.EpicSpellFocusAbjuration!) => 6,
            _ when caster.KnowsFeat(Feat.GreaterSpellFocusAbjuration!) => 3,
            _ when caster.KnowsFeat(Feat.SpellFocusAbjuration!) => 1,
            _ => 0
        };

        return Math.Min(casterLevel + bonus, MaxCasterLevel);
    }

    private void DoTargetedDispel(NwCreature caster, NwGameObject target, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        SpellUtils.SignalSpell(caster, target, spell);

        if (target is NwCreature targetCreature && HasTimeStop(targetCreature))
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
            return;
        }

        if (IsDispelImmune(target)) return;

        target.ApplyEffect(EffectDuration.Instant, impactVfx);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicAll(casterLevel), target.ObjectId);
        target.ApplyEffect(EffectDuration.Instant, breachVfx);
    }

    private void DoAreaDispel(NwCreature caster, Location targetLocation, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        targetLocation.ApplyEffect(EffectDuration.Instant, impactVfx);

        IntPtr targetLoc = GetSpellTargetLocation();
        const int validTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_AREA_OF_EFFECT | OBJECT_TYPE_PLACEABLE;

        uint current = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validTypes);
        while (GetIsObjectValid(current) == TRUE)
        {
            ProcessAreaTarget(caster, current, casterLevel, breachVfx, spell);
            current = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validTypes);
        }
    }

    private void ProcessAreaTarget(NwCreature caster, uint currentTarget, int casterLevel,
        Effect breachVfx, NwSpell spell)
    {
        switch (GetObjectType(currentTarget))
        {
            case OBJECT_TYPE_AREA_OF_EFFECT:
                dispelService.TryDispelAreaOfEffect(caster, currentTarget, casterLevel);
                break;

            case OBJECT_TYPE_PLACEABLE:
                SignalEvent(currentTarget, EventSpellCastAt(caster.ObjectId, SPELL_DISPEL_MAGIC));
                break;

            case OBJECT_TYPE_CREATURE:
                NwCreature? creature = currentTarget.ToNwObject<NwCreature>();
                if (creature == null) break;

                SpellUtils.SignalSpell(caster, creature, spell);

                if (HasTimeStop(creature))
                    creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
                else if (!IsDispelImmune(creature))
                {
                    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicBest(casterLevel), currentTarget);
                    creature.ApplyEffect(EffectDuration.Instant, breachVfx);
                }

                break;
        }
    }

    private static bool HasTimeStop(NwCreature creature)
        => creature.ActiveEffects.Any(e => e.Spell?.SpellType == Spell.TimeStop);

    private static bool IsDispelImmune(NwGameObject target)
        => NwEffects.GetHasEffectType(EFFECT_TYPE_PETRIFY, target.ObjectId) == TRUE
           || GetLocalInt(target.ObjectId, "X1_L_IMMUNE_TO_DISPEL") == 10;
}
