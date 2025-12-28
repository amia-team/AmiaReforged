using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Arcane.SixthCircle.Abjuration;

/// <summary>
/// Greater Dispelling - A powerful abjuration spell that removes magical effects.
///
/// When cast on a single target, dispels all effects on that target.
/// When cast on an area, dispels the best (highest CL) effect on each creature.
///
/// Abjuration spell focus feats provide bonuses to caster level:
/// - Spell Focus: +1 CL
/// - Greater Spell Focus: +3 CL
/// - Epic Spell Focus: +6 CL
///
/// Maximum effective caster level is capped at 30.
/// Creatures under Time Stop cannot be dispelled.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class GreaterDispelling : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_GrDispel";

    private const int MaxCasterLevel = 30;

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        NwGameObject? targetObject = eventData.TargetObject;
        Location? targetLocation = eventData.TargetLocation;

        int casterLevel = CalculateEffectiveCasterLevel(caster);

        // Send debug messages to player
        SendCasterLevelInfo(caster, eventData.Caster.CasterLevel, casterLevel);

        Effect breachVfx = Effect.VisualEffect(VfxType.ImpBreach);
        Effect impactVfx = Effect.VisualEffect(VfxType.FnfDispelGreater);

        if (targetObject != null)
        {
            // Targeted Dispel - Dispel all effects
            DoTargetedDispel(caster, targetObject, casterLevel, breachVfx, impactVfx, eventData.Spell);
        }
        else if (targetLocation != null)
        {
            // Area of Effect - Dispel best effect only
            DoAreaDispel(caster, targetLocation, casterLevel, breachVfx, impactVfx, eventData.Spell);
        }
    }

    private static int CalculateEffectiveCasterLevel(NwCreature caster)
    {
        int casterLevel = caster.CasterLevel;
        int bonus = 0;

        // Check for Abjuration spell focus feats (cumulative bonuses based on highest feat)
        if (caster.KnowsFeat(Feat.EpicSpellFocusAbjuration!))
        {
            bonus = 6;
        }
        else if (caster.KnowsFeat(Feat.GreaterSpellFocusAbjuration!))
        {
            bonus = 3;
        }
        else if (caster.KnowsFeat(Feat.SpellFocusAbjuration!))
        {
            bonus = 1;
        }

        casterLevel += bonus;

        // Cap at maximum caster level
        return Math.Min(casterLevel, MaxCasterLevel);
    }

    private static void SendCasterLevelInfo(NwCreature caster, int startingCl, int modifiedCl)
    {
        NwPlayer? player = caster.ControllingPlayer;
        if (player == null) return;

        int bonus = modifiedCl - startingCl;
        player.SendServerMessage($"Starting CL: {startingCl}");
        player.SendServerMessage($"Bonus: {bonus}");
        player.SendServerMessage($"Modified CL: {modifiedCl}");
    }

    private static void DoTargetedDispel(NwCreature caster, NwGameObject target, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        // Signal spell cast at event
        SpellUtils.SignalSpell(caster, target, spell);

        // Can't dispel Time Stopped creatures
        if (target is NwCreature targetCreature && HasTimeStop(targetCreature))
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
            return;
        }

        // Check for dispel immunity
        if (IsDispelImmune(target))
        {
            return;
        }

        // Apply visual effects
        target.ApplyEffect(EffectDuration.Instant, impactVfx);

        // Dispel all effects on the target
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicAll(casterLevel), target.ObjectId);
        target.ApplyEffect(EffectDuration.Instant, breachVfx);
    }

    private static void DoAreaDispel(NwCreature caster, Location targetLocation, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        // Apply area impact visual
        targetLocation.ApplyEffect(EffectDuration.Instant, impactVfx);

        uint casterObjectId = caster.ObjectId;

        // Use the target location for the shape
        IntPtr targetLoc = GetSpellTargetLocation();

        const int validObjectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_AREA_OF_EFFECT | OBJECT_TYPE_PLACEABLE;
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validObjectTypes);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            int objectType = GetObjectType(currentTarget);

            if (objectType == OBJECT_TYPE_AREA_OF_EFFECT)
            {
                // Handle Area of Effect objects
                DispelAreaOfEffect(currentTarget, casterLevel);
            }
            else if (objectType == OBJECT_TYPE_PLACEABLE)
            {
                // Signal event for placeables
                SignalEvent(currentTarget, EventSpellCastAt(casterObjectId, SPELL_GREATER_DISPELLING));
            }
            else if (objectType == OBJECT_TYPE_CREATURE)
            {
                // Handle creatures
                NwCreature? creature = currentTarget.ToNwObject<NwCreature>();
                if (creature != null)
                {
                    SpellUtils.SignalSpell(caster, creature, spell);

                    // Can't dispel Time Stopped creatures
                    if (HasTimeStop(creature))
                    {
                        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
                    }
                    else if (!IsDispelImmune(creature))
                    {
                        // Dispel best effect only in AoE mode
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicBest(casterLevel), currentTarget);
                        creature.ApplyEffect(EffectDuration.Instant, breachVfx);
                    }
                }
            }

            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validObjectTypes);
        }
    }

    private static void DispelAreaOfEffect(uint aoeObject, int casterLevel)
    {
        // Get the creator's caster level
        uint aoeCreator = GetAreaOfEffectCreator(aoeObject);
        int aoeCreatorCl = GetCasterLevel(aoeCreator);

        // Check if it's a mobile aura (can't dispel these)
        string tag = GetTag(aoeObject);
        bool isAura = tag.Length >= 7 && GetSubString(tag, 0, 7) == "VFX_MOB";

        if (!isAura)
        {
            // Perform dispel check
            if (NwEffects.DispelCheck(casterLevel, aoeCreatorCl) == TRUE)
            {
                DestroyObject(aoeObject);
            }
        }
    }

    private static bool HasTimeStop(NwCreature creature)
    {
        return creature.ActiveEffects.Any(e =>
            e.Spell?.SpellType == Spell.TimeStop);
    }

    private static bool IsDispelImmune(NwGameObject target)
    {
        uint targetId = target.ObjectId;

        // Check for petrification (can't dispel petrified creatures)
        if (NwEffects.GetHasEffectType(EFFECT_TYPE_PETRIFY, targetId) == TRUE)
        {
            return true;
        }

        // Check for dispel immunity local variable
        if (GetLocalInt(targetId, "X1_L_IMMUNE_TO_DISPEL") == 10)
        {
            return true;
        }

        return false;
    }
}

