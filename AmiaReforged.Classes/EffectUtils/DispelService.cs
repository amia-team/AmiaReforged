using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils;

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

        List<DispelInfo> spellsToDispel = [];

        spellsToDispel.AddRange(GetEffectsBySpell(target).Select(effects => new DispelInfo
        (
            Name: effects[0].Spell?.Name.ToString() ?? "Unknown Spell",
            InnateLevel: effects[0].Spell?.InnateSpellLevel ?? 0,
            CasterLevel: effects[0].CasterLevel,
            DispelAction: () => TryDispelEffect(target, effects, dispelModifier)
        )));

        spellsToDispel.AddRange(GetItemPropertiesBySpell(target).Select(pair => new DispelInfo
        (
            Name: pair.ItemProperties[0].Spell?.Name.ToString() ?? "Unknown Spell",
            InnateLevel: pair.ItemProperties[0].Spell?.InnateSpellLevel ?? 0,
            CasterLevel: pair.ItemProperties[0].CasterLevel,
            DispelAction: () => TryDispelItemProperty(pair.Item, pair.ItemProperties, dispelModifier)
        )));

        spellsToDispel = spellsToDispel
            .OrderByDescending(t => t.InnateLevel)
            .ThenByDescending(t => t.CasterLevel)
            .ToList();

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
    /// <param name="spell">Spell which decides the cap, defaults to 10 if it's not recognised</param>
    public int GetDispelModifier(NwGameObject caster, int casterLevel, NwSpell spell)
    {
        int casterLevelCap = GetCasterLevelCap(spell.SpellType);
        int dispelModifier = Math.Min(casterLevel, casterLevelCap);

        if (caster is NwCreature casterCreature)
        {
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
    private static bool TryDispelEffect(NwGameObject target, Effect[] spellEffects, int dispelModifier)
    {
        int effectCasterLevel = spellEffects[0].CasterLevel;

        if (!RollDispelCheck(dispelModifier, effectCasterLevel)) return false;

        foreach (Effect effect in spellEffects)
        {
            target.RemoveEffect(effect);
        }

        return true;
    }

    /// <summary>
    /// Performs a dispel check against a specific spell effect on a target.
    /// Uses the formula: d20 + dispel modifier >= 12 + effect caster level
    /// </summary>
    /// <param name="targetItem">The target item to dispel the properties from</param>
    /// <param name="itemProperties">The temporary properties grouped by spell and caster checked for dispelling</param>
    /// <param name="dispelModifier">The caster's modifier for the dispel check</param>
    /// <returns>True if the effect group is successfully dispelled, otherwise false</returns>
    private static bool TryDispelItemProperty(NwItem targetItem, ItemProperty[] itemProperties, int dispelModifier)
    {
        int effectCasterLevel = itemProperties[0].CasterLevel;

        if (!RollDispelCheck(dispelModifier, effectCasterLevel)) return false;

        foreach (ItemProperty itemProperty in itemProperties)
        {
            targetItem.RemoveItemProperty(itemProperty);
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
    /// Gets the caster level cap for a dispel type, defaults to 10 if it's not recognised.
    /// Lesser Dispel = 5, Dispel Magic = 10, Greater Dispel = 15, Devour Magic = 20, Mordenkainen's Disjunction = 30.
    /// </summary>
    private static int GetCasterLevelCap(Spell spell) => spell switch
    {
        Spell.LesserDispel => 5,
        Spell.DispelMagic => 10,
        Spell.GreaterDispelling => 15,
        DevourMagic => 20,
        Spell.MordenkainensDisjunction => 30,
        _ => 10
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
    /// Return a list of all effects on a target, grouped by spell and creator.
    /// </summary>
    private static List<Effect[]> GetEffectsBySpell(NwGameObject target) => target.ActiveEffects
        .Where(e => e is { Spell: not null, SubType: EffectSubType.Magical,
            EffectType: not (EffectType.SummonCreature or EffectType.Swarm) })
        .GroupBy(e => new {e.Spell, e.Creator})
        .Select(group => group.ToArray())
        .ToList();

    /// <summary>
    /// Return a list of all temporary item properties on a target, grouped by item, spell, and creator.
    /// </summary>
    private static List<(NwItem Item, ItemProperty[] ItemProperties)> GetItemPropertiesBySpell(NwGameObject target)
        => GetEquippedItems(target)
            .SelectMany(item => item.ItemProperties, (item, ip) => (Item: item, ItemProperty: ip))
            .Where(x => x.ItemProperty is { Spell: not null, DurationType: EffectDuration.Temporary })
            .GroupBy(x => new { x.Item, x.ItemProperty.Spell, x.ItemProperty.Creator })
            .Select(group => (
                group.Key.Item,
                ItemProperties: group.Select(x => x.ItemProperty).ToArray()
            ))
            .ToList();

    private static readonly InventorySlot[] AllSlots = Enum.GetValues<InventorySlot>();

    /// <summary>
    /// Gets all items currently equipped by the target creature.
    /// </summary>
    private static NwItem[] GetEquippedItems(NwGameObject target)
    {
        if (target is not NwCreature creature)
            return [];

        List<NwItem> equippedItems = [];

        foreach (InventorySlot slot in AllSlots)
        {
            NwItem? item = creature.GetItemInSlot(slot);
            if (item != null)
                equippedItems.Add(item);
        }

        return equippedItems.ToArray();
    }
}
