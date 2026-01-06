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
/// PC Casting uses enhanced spell focus bonuses:
/// - Spell Focus: +1 CL
/// - Greater Spell Focus: +3 CL
/// - Epic Spell Focus: +6 CL
/// - Maximum effective caster level is capped at 30.
///
/// NPC Casting uses the legacy dispel system:
/// - Caster level capped at 15
/// - Feat bonuses: +2 per abjuration focus tier
/// - Per-effect dispel checks with player feedback
/// - PvP bonus randomizer system
///
/// Creatures under Time Stop cannot be dispelled.
/// Petrified creatures and those with X1_L_IMMUNE_TO_DISPEL=10 are immune.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class GreaterDispelling : ISpell
{
    private const int MaxCasterLevelPc = 30;
    private const string PcDispelOverrideVar = "PCDispel";

    private readonly DispelService _dispelService;

    public GreaterDispelling(DispelService dispelService)
    {
        _dispelService = dispelService;
    }

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_GrDispel";

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        NwGameObject? targetObject = eventData.TargetObject;
        Location? targetLocation = eventData.TargetLocation;

        // Determine if we use PC or NPC casting logic
        bool usePcCasting = ShouldUsePcCasting(caster);

        if (usePcCasting)
        {
            DoPcCasting(caster, targetObject, targetLocation, eventData.Spell);
        }
        else
        {
            DoNpcCasting(caster, targetObject, targetLocation, eventData.Spell);
        }
    }

    /// <summary>
    /// Determines whether to use PC casting rules (enhanced) or NPC rules (legacy balanced).
    /// </summary>
    private static bool ShouldUsePcCasting(NwCreature caster)
    {
        // PCs and DMs use the enhanced version
        if (caster.IsPlayerControlled || caster.IsDMAvatar)
        {
            return true;
        }

        // NPCs can be flagged to use PC version via local variable
        if (GetLocalInt(caster.ObjectId, PcDispelOverrideVar) == 1)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// PC casting uses enhanced spell focus bonuses and higher CL cap.
    /// </summary>
    private void DoPcCasting(NwCreature caster, NwGameObject? targetObject, Location? targetLocation, NwSpell spell)
    {
        int casterLevel = CalculatePcEffectiveCasterLevel(caster);

        SendCasterLevelInfo(caster, caster.CasterLevel, casterLevel);

        Effect breachVfx = Effect.VisualEffect(VfxType.ImpBreach);
        Effect impactVfx = Effect.VisualEffect(VfxType.FnfDispelGreater);

        if (targetObject != null)
        {
            DoPcTargetedDispel(caster, targetObject, casterLevel, breachVfx, impactVfx, spell);
        }
        else if (targetLocation != null)
        {
            DoPcAreaDispel(caster, targetLocation, casterLevel, breachVfx, impactVfx, spell);
        }
    }

    private static int CalculatePcEffectiveCasterLevel(NwCreature caster)
    {
        int casterLevel = caster.CasterLevel;
        int bonus = 0;

        // PC version: non-cumulative bonuses based on highest feat
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

        return Math.Min(casterLevel, MaxCasterLevelPc);
    }

    private void DoPcTargetedDispel(NwCreature caster, NwGameObject target, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        SpellUtils.SignalSpell(caster, target, spell);

        if (target is NwCreature targetCreature && HasTimeStop(targetCreature))
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
            return;
        }

        if (IsDispelImmune(target))
        {
            return;
        }

        target.ApplyEffect(EffectDuration.Instant, impactVfx);

        // PC version uses engine's EffectDispelMagicAll
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicAll(casterLevel), target.ObjectId);
        target.ApplyEffect(EffectDuration.Instant, breachVfx);
    }

    private void DoPcAreaDispel(NwCreature caster, Location targetLocation, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        targetLocation.ApplyEffect(EffectDuration.Instant, impactVfx);

        IntPtr targetLoc = GetSpellTargetLocation();

        const int validObjectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_AREA_OF_EFFECT | OBJECT_TYPE_PLACEABLE;
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validObjectTypes);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            ProcessPcAreaTarget(caster, currentTarget, casterLevel, breachVfx, spell);
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validObjectTypes);
        }
    }

    private void ProcessPcAreaTarget(NwCreature caster, uint currentTarget, int casterLevel, Effect breachVfx, NwSpell spell)
    {
        int objectType = GetObjectType(currentTarget);

        switch (objectType)
        {
            case OBJECT_TYPE_AREA_OF_EFFECT:
                _dispelService.TryDispelAreaOfEffect(caster, currentTarget, casterLevel);
                break;

            case OBJECT_TYPE_PLACEABLE:
                SignalEvent(currentTarget, EventSpellCastAt(caster.ObjectId, SPELL_GREATER_DISPELLING));
                break;

            case OBJECT_TYPE_CREATURE:
                NwCreature? creature = currentTarget.ToNwObject<NwCreature>();
                if (creature != null)
                {
                    SpellUtils.SignalSpell(caster, creature, spell);

                    if (HasTimeStop(creature))
                    {
                        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
                    }
                    else if (!IsDispelImmune(creature))
                    {
                        // PC AoE mode: dispel best effect only
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicBest(casterLevel), currentTarget);
                        creature.ApplyEffect(EffectDuration.Instant, breachVfx);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// NPC casting uses the legacy dispel system with CL cap of 15 and custom dispel checks.
    /// This prevents balance issues with old dungeons that rely on specific dispel mechanics.
    /// </summary>
    private void DoNpcCasting(NwCreature caster, NwGameObject? targetObject, Location? targetLocation, NwSpell spell)
    {
        int casterLevel = caster.CasterLevel;

        SendCasterLevelInfo(caster, caster.CasterLevel, casterLevel);

        Effect breachVfx = Effect.VisualEffect(VfxType.ImpBreach);
        Effect impactVfx = Effect.VisualEffect(VfxType.FnfDispelGreater);

        if (targetObject != null)
        {
            DoNpcTargetedDispel(caster, targetObject, casterLevel, breachVfx, impactVfx, spell);
        }
        else if (targetLocation != null)
        {
            DoNpcAreaDispel(caster, targetLocation, casterLevel, breachVfx, impactVfx, spell);
        }
    }

    private void DoNpcTargetedDispel(NwCreature caster, NwGameObject target, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        SpellUtils.SignalSpell(caster, target, spell);

        if (target is NwCreature targetCreature && HasTimeStop(targetCreature))
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
            return;
        }

        if (IsDispelImmune(target))
        {
            return;
        }

        target.ApplyEffect(EffectDuration.Instant, impactVfx);

        // NPC version: uses custom DispelService with per-effect checks and CL cap of 15
        // maxSpells=4 matches the original NWScript: DispelEffectsAll(OBJECT_SELF, nCasterLevel, oTarget, 4, SPELL_GREATER_DISPELLING)
        _dispelService.DispelEffectsAll(caster, target, casterLevel, DispelService.DispelType.GreaterDispelling, maxSpells: 4);

        target.ApplyEffect(EffectDuration.Instant, breachVfx);
    }

    private void DoNpcAreaDispel(NwCreature caster, Location targetLocation, int casterLevel,
        Effect breachVfx, Effect impactVfx, NwSpell spell)
    {
        targetLocation.ApplyEffect(EffectDuration.Instant, impactVfx);

        IntPtr targetLoc = GetSpellTargetLocation();

        const int validObjectTypes = OBJECT_TYPE_CREATURE | OBJECT_TYPE_AREA_OF_EFFECT | OBJECT_TYPE_PLACEABLE;
        uint currentTarget = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validObjectTypes);

        while (GetIsObjectValid(currentTarget) == TRUE)
        {
            ProcessNpcAreaTarget(caster, currentTarget, casterLevel, breachVfx, spell);
            currentTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_LARGE, targetLoc, FALSE, validObjectTypes);
        }
    }

    private void ProcessNpcAreaTarget(NwCreature caster, uint currentTarget, int casterLevel, Effect breachVfx, NwSpell spell)
    {
        int objectType = GetObjectType(currentTarget);

        switch (objectType)
        {
            case OBJECT_TYPE_AREA_OF_EFFECT:
                _dispelService.TryDispelAreaOfEffect(caster, currentTarget, casterLevel);
                break;

            case OBJECT_TYPE_PLACEABLE:
                SignalEvent(currentTarget, EventSpellCastAt(caster.ObjectId, SPELL_GREATER_DISPELLING));
                break;

            case OBJECT_TYPE_CREATURE:
                NwCreature? creature = currentTarget.ToNwObject<NwCreature>();
                if (creature != null)
                {
                    SpellUtils.SignalSpell(caster, creature, spell);

                    if (HasTimeStop(creature))
                    {
                        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
                    }
                    else if (!IsDispelImmune(creature))
                    {
                        // NPC AoE mode: use engine dispel for best effect (matches original behavior)
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDispelMagicBest(casterLevel), currentTarget);
                        creature.ApplyEffect(EffectDuration.Instant, breachVfx);
                    }
                }
                break;
        }
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

    private static bool HasTimeStop(NwCreature creature)
    {
        return creature.ActiveEffects.Any(e => e.Spell?.SpellType == Spell.TimeStop);
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

