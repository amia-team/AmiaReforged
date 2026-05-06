using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.Dispel;

/// <summary>
/// Service for handling dispel magic mechanics with custom logic matching Amia's dispel system.
/// Provides per-effect dispel checks, feedback to players, and item property dispel.
/// </summary>
[ServiceBinding(typeof(DispelService))]
public class DispelService(ScriptHandleFactory scriptHandleFactory)
{
    /// <summary>
    /// Information about a spell or effect to be dispelled.
    /// </summary>
    /// <param name="Name">The name of the spell.</param>
    /// <param name="InnateLevel">The innate spell level of the spell.</param>
    /// <param name="CasterLevel">The caster level of the spell when it was cast.</param>
    /// <param name="DispelAction">A function that attempts to dispel the effect and returns true if successful.</param>
    private record DispelInfo(string Name, int InnateLevel, int CasterLevel, Func<bool> DispelAction);

    /// <summary>
    /// Information about a single successful dispel to be reported back to the caster and target.
    /// </summary>
    /// <param name="Target">The object the effect was dispelled from.</param>
    /// <param name="SpellName">The name of the dispelled spell.</param>
    /// <param name="EffectCasterLevel">The caster level of the dispelled effect.</param>
    private record DispelFeedbackEntry(NwGameObject Target, string SpellName, int EffectCasterLevel);

    private readonly Dictionary<Guid, List<DispelFeedbackEntry>> _dispelFeedbackCache = [];

    /// <summary>
    /// Creates a dispel magic effect that, when applied, will attempt to dispel spells from the target.
    /// </summary>
    /// <param name="dispelModifier">The caster's dispel modifier, use DispelService's GetDispelModifier.</param>
    /// <param name="caster">The caster of the dispel magic.</param>
    /// <param name="maxSpells">The maximum number of spells to dispel. 0 means dispel all spells.</param>
    /// <returns>A new <see cref="Effect"/> of type RunAction that handles the dispel logic.</returns>
    public Effect DispelMagic(int dispelModifier, NwGameObject caster, int maxSpells = 0)
    {
        ScriptCallbackHandle onApplied = scriptHandleFactory.CreateUniqueHandler(info =>
            OnDispelApply(info, dispelModifier, caster, maxSpells));

        Effect dispelMagic = Effect.RunAction(onAppliedHandle: onApplied);
        dispelMagic.SubType = EffectSubType.Magical;

        return dispelMagic;
    }

    private ScriptHandleResult OnDispelApply(CallInfo info, int dispelModifier, NwGameObject caster, int maxSpells)
    {
        if (info.ObjectSelf is not NwGameObject target) return ScriptHandleResult.Handled;

        if (!_dispelFeedbackCache.TryGetValue(caster.UUID, out List<DispelFeedbackEntry>? dispelFeedback))
        {
            dispelFeedback = [];
            _dispelFeedbackCache[caster.UUID] = dispelFeedback;
        }

        if (target is NwAreaOfEffect aoeObject)
        {
            if (TryDispelAreaOfEffect(aoeObject, dispelModifier, caster))
            {
                string aoeName = aoeObject.Spell?.Name.ToString() ?? "Unknown Spell";
                dispelFeedback.Add(new DispelFeedbackEntry(target, aoeName, aoeObject.CasterLevel));
                _dispelFeedbackCache[caster.UUID] = dispelFeedback;
            }

            return ScriptHandleResult.Handled;
        }

        List<(NwGameObject Owner, Effect Effect)[]> sortedEffects = GetSortedEffects(target);

        List<DispelInfo> spellsToDispel = sortedEffects.Select(effects => new DispelInfo
        (
            Name: effects[0].Effect.Spell?.Name.ToString() ?? "Unknown Spell",
            InnateLevel: effects[0].Effect.Spell?.InnateSpellLevel ?? 0,
            CasterLevel: effects[0].Effect.CasterLevel,
            DispelAction: () => TryDispelEffect(effects, dispelModifier)
        )).ToList();

        foreach (DispelInfo dispelInfo in spellsToDispel)
        {
            if (maxSpells > 0 && dispelFeedback.Count >= maxSpells)
                break;

            if (dispelInfo.DispelAction.Invoke())
                dispelFeedback.Add(new DispelFeedbackEntry(target, dispelInfo.Name, dispelInfo.CasterLevel));
        }

        _dispelFeedbackCache[caster.UUID] = dispelFeedback;
        return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// Sends a summary of dispelled effects to the caster and the target(s).
    /// This must be called only after the dispel magic effect has been applied to all desired game objects.
    /// A delay should not be used with this, unless you apply the dispel magic effects with a delay.
    /// This also flushes the dispel feedback cache, so use this at the end of every dispel magic spell script.
    /// </summary>
    /// <param name="caster">The caster of the dispel magic.</param>
    /// <returns>The total number of spells/effects dispelled (if you want that count for something).</returns>
    public int FlushDispelFeedback(NwCreature caster)
    {
        int dispelCount = 0;
        if (!_dispelFeedbackCache.Remove(caster.UUID, out List<DispelFeedbackEntry>? dispelFeedback))
            return dispelCount;

        dispelCount = dispelFeedback.Count;
        if (dispelCount == 0) return dispelCount;

        List<DispelFeedbackEntry[]> feedbackByTarget = dispelFeedback
            .GroupBy(entry => entry.Target)
            .Select(entry => entry.ToArray())
            .ToList();

        if (caster.IsPlayerControlled(out NwPlayer? casterPlayer))
        {
            string casterMessage = $"{caster.Name} dispelled magic from:".ColorString(ColorConstants.Magenta);

            List<DispelFeedbackEntry> dispelledAoes = feedbackByTarget
                .Where(entry => entry[0].Target is NwAreaOfEffect)
                .SelectMany(entry => entry)
                .ToList();

            if (dispelledAoes.Count > 0)
            {
                casterMessage += "\n Area of Effects:".ColorString(ColorConstants.Cyan);

                foreach (DispelFeedbackEntry dispelledAoe in dispelledAoes)
                {
                    casterMessage += $"\n - {dispelledAoe.SpellName} ({dispelledAoe.EffectCasterLevel})"
                        .ColorString(ColorConstants.Gray);
                }
            }

            foreach (DispelFeedbackEntry[] dispelFeedbackEntries in feedbackByTarget)
            {
                NwGameObject targetObject = dispelFeedbackEntries[0].Target;
                if (targetObject is NwAreaOfEffect) continue;

                casterMessage += $"\n {targetObject.Name}:".ColorString(ColorConstants.Cyan);

                foreach (DispelFeedbackEntry entry in dispelFeedbackEntries)
                {
                    casterMessage += $"\n - {entry.SpellName} ({entry.EffectCasterLevel})"
                        .ColorString(ColorConstants.Gray);
                }
            }
            casterPlayer.SendServerMessage(casterMessage);
        }

        foreach (DispelFeedbackEntry[] dispelFeedbackEntries in feedbackByTarget)
        {
            NwGameObject targetObject = dispelFeedbackEntries[0].Target;

            if (!targetObject.IsPlayerControlled(out NwPlayer? targetPlayer))
                continue;

            string targetMessage = $"{caster.Name} dispelled magic from you:".ColorString(ColorConstants.Magenta);

            foreach (DispelFeedbackEntry entry in dispelFeedbackEntries)
            {
                targetMessage += $"\n - {entry.SpellName} ({entry.EffectCasterLevel})"
                    .ColorString(ColorConstants.Gray);
            }

            targetPlayer.SendServerMessage(targetMessage);
        }

        return dispelCount;
    }

    /// <summary>
    /// A util signal to be used before DispelTarget in a spell script. Signals harmful spells to neutral and hostile
    /// creatures; signals harmless spell to friendly creatures and objects
    /// </summary>
    /// <param name="caster">Caster of dispel</param>
    /// <param name="target">Target of dispel</param>
    /// <param name="spell">Spell gotten from the OnSpellCast event data</param>
    public void SignalDispel(NwCreature caster, NwGameObject target, NwSpell spell)
    {
        if (target is NwCreature creature && !caster.IsReactionTypeFriendly(creature))
            SpellUtils.SignalSpell(caster, creature, spell);
        else SpellUtils.SignalSpell(caster, target, spell, harmful: false);
    }

    /// <summary>
    /// Gets the dispel modifier for the dispel type, used for dispel checks.
    /// Formula: d20 + CL (max dispel spell's cap) + 2 per Abjuration Focus feat.
    /// </summary>
    /// <param name="caster">Caster who is casting the dispel</param>
    /// <param name="casterLevel">Caster level, usually got from the spell event's data</param>
    /// <param name="spell">Spell which decides the cap, defaults to 0 if it's not recognised</param>
    public int GetDispelModifier(NwGameObject caster, int casterLevel, NwSpell spell)
    {
        int dispelModifier = GetCasterLevelByDispel(spell.SpellType, casterLevel);

        if (caster is NwCreature casterCreature)
        {
            if (dispelModifier == 0)
                casterCreature.ControllingPlayer?.SendServerMessage
                    ("Dispel modifier not recognised, please report to a dev.".ColorString(ColorConstants.Red));

            int featBonus = GetAbjurationFocusBonus(casterCreature);
            dispelModifier += featBonus;
        }

        return dispelModifier;
    }

    /// <summary>
    /// Checks for conditions that make the target immune to dispel and plays the ImpGlobeUse VFX if immune.
    /// </summary>
    /// <param name="targetObject">Target to dispel</param>
    /// <returns>True if the target is immune to dispel, otherwise false</returns>
    public bool IsImmuneToDispel(NwGameObject targetObject)
    {
        // Petrified or timestopped objects or objects marked for dispel immune with a local int
        if (targetObject.GetObjectVariable<LocalVariableInt>("X1_L_IMMUNE_TO_DISPEL").Value == 10
            || targetObject.ActiveEffects.Any(e => e.Spell?.SpellType == Spell.TimeStop
                                                   || e.EffectType == EffectType.Petrify))
        {
            targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpGlobeUse));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs a dispel check against an Area of Effect object.
    /// </summary>
    /// <returns>True if the AoE was successfully dispelled or if the AoE was cast by the caster</returns>
    private bool TryDispelAreaOfEffect(NwAreaOfEffect aoeObject, int dispelModifier, NwGameObject caster)
    {
        // Don't dispel mobility type AoEs, those are dispelled if the dispel is cast on the target itself
        if (aoeObject.Tag[..7] == "VFX_MOB")
        {
            return false;
        }

        if (aoeObject.Creator == caster || RollDispelCheck(dispelModifier, aoeObject.CasterLevel))
        {
            aoeObject.Destroy();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs a dispel check against a specific spell effect on a target.
    /// Uses the formula: d20 + dispel modifier >= 12 + effect caster level
    /// If the effect doesn't have a caster level, uses the dispelled creature's HD instead.
    /// </summary>
    /// <param name="target">The target to dispel the effect from</param>
    /// <param name="spellEffects">The effects grouped by spell and caster checked for dispelling</param>
    /// <param name="dispelModifier">The caster's modifier for the dispel check</param>
    /// <returns>True if the effect group is successfully dispelled, otherwise false</returns>
    private static bool TryDispelEffect((NwGameObject Owner, Effect Effect)[] spellEffects, int dispelModifier)
    {
        int effectCasterLevel = spellEffects[0].Effect.CasterLevel;

        if (!RollDispelCheck(dispelModifier, effectCasterLevel)) return false;

        // Remove the effect from its specific owner (creature or item)
        foreach ((NwGameObject owner, Effect effect) in spellEffects)
        {
            owner.RemoveEffect(effect);
        }

        return true;
    }

    /// <summary>
    /// Rolls a dispel check of D20 + Dispel CL vs. 12 + Effect Caster Level
    /// </summary>
    /// <param name="dispelModifier">The caster's dispel modifier</param>
    /// <param name="targetEffectCl">The effective CL of the target effect being removed</param>
    /// <returns>True if the dispel check is greater or equal than the dispel resistance, otherwise false</returns>
    private static bool RollDispelCheck(int dispelModifier, int targetEffectCl)
        => Random.Shared.Roll(20) + dispelModifier >= 12 + targetEffectCl;

    private const Spell DevourMagic = (Spell)1014;

    /// <summary>
    /// Gets the caster level scaling by dispel, defaults to 0 if the spell's not recognised.
    /// Lesser Dispel 1:3, Dispel Magic 1:2, Greater Dispel 2:3, Devour Magic 2:3, Mordenkainen's Disjunction 1:1
    /// </summary>
    private static int GetCasterLevelByDispel(Spell spell, int casterLevel) => spell switch
    {
        Spell.LesserDispel => casterLevel / 3,
        Spell.DispelMagic => casterLevel / 2,
        Spell.GreaterDispelling => casterLevel * 2 / 3,
        DevourMagic => casterLevel * 2 / 3,
        Spell.MordenkainensDisjunction => casterLevel,
        _ => 0
    };

    /// <summary>
    /// Gets feat bonus for abjuration focus (+2 per tier).
    /// </summary>
    private static int GetAbjurationFocusBonus(NwCreature caster) =>
        caster.KnowsFeat(NwFeat.FromFeatType(Feat.EpicSpellFocusAbjuration)!) ? 6
        : caster.KnowsFeat(NwFeat.FromFeatType(Feat.GreaterSpellFocusAbjuration)!) ? 4
        : caster.KnowsFeat(NwFeat.FromFeatType(Feat.SpellFocusAbjuration)!) ? 2
        : 0;

    /// <summary>
    /// Return a list of all effects on a target and its equipped items, grouped by spell/creator and sorted by Innate Level then Caster Level.
    /// </summary>
    /// <summary>
    /// Return a list of all effects on a target and its equipped items, grouped by spell/creator and sorted by Innate Level then Caster Level.
    /// </summary>
    private static List<(NwGameObject Owner, Effect Effect)[]> GetSortedEffects(NwGameObject target)
    {
        // Tag creature effects with the creature as the owner
        IEnumerable<(NwGameObject Owner, Effect Effect)> allEffects = target.ActiveEffects.Select(e => (target, e));

        if (target is NwCreature creature)
        {
            // Tag item effects with the item as the owner
            IEnumerable<(NwGameObject Owner, Effect Effect)> itemEffects = AllSlots
                .Select(creature.GetItemInSlot)
                .Where(item => item != null)
                .SelectMany(item => item!.ActiveEffects.Select(e => ((NwGameObject)item, e)));

            allEffects = allEffects.Concat(itemEffects);
        }

        return allEffects
            .Where(x => x.Effect is
            {
                Spell: not null, SubType: EffectSubType.Magical,
                EffectType: not (EffectType.SummonCreature or EffectType.Swarm)
            })
            .GroupBy(x => new { x.Effect.Spell, x.Effect.Creator, x.Effect.CasterLevel })
            .OrderByDescending(g => g.Key.Spell!.InnateSpellLevel)
            .ThenByDescending(g => g.Key.CasterLevel)
            .Select(group => group.ToArray())
            .ToList();
    }

    private static readonly InventorySlot[] AllSlots = Enum.GetValues<InventorySlot>();
}
