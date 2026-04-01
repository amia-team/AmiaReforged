using AmiaReforged.Classes.Spells;
using Anvil.API;
using NWN.Core;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils;

/// <summary>
/// Service for handling dispel magic mechanics with custom logic matching Amia's dispel system.
/// Provides per-effect dispel checks, PvP bonuses, and detailed feedback to players.
/// </summary>
[ServiceBinding(typeof(DispelService))]
public class DispelService
{
    /// <summary>
    /// Dispel type identifiers matching NWN spell ID.
    /// </summary>
    public enum DispelType
    {
        LesserDispel = 165,
        DispelMagic = 41,
        GreaterDispelling = 67,
        MordenkainensDisjunction = 112,
        DevourMagic = 1014
    }

    /// <summary>
    /// Dispels all or a set number of spells on a target using Amia's custom dispel system.
    /// Runs dispel checks per magical effects and temporary item properties, grouped by spell and creator.
    /// Starts from the highest spell level and caster level, working down to the lowest.
    /// Prints a list of dispelled spells in the combat log to the caster and target.
    /// </summary>
    /// <param name="caster">The creature casting the dispel</param>
    /// <param name="target">The target to dispel effects from</param>
    /// <param name="casterLevel">The effective caster level for dispel checks</param>
    /// <param name="dispelType">The type of dispel spell being used</param>
    /// <param name="maxSpells">Maximum number of spells to dispel (0 = unlimited)</param>
    /// <returns>The number of spells successfully dispelled</returns>
    public int DispelEffectsAll(NwCreature caster, NwGameObject target, int casterLevel, DispelType dispelType, int maxSpells = 0)
    {
        // 1. Signal spell to target
        if (target is NwCreature targetCreature && caster.IsReactionTypeHostile(targetCreature))
        {
            SpellUtils.SignalSpell(caster, targetCreature, NwSpell.FromSpellId((int)dispelType)!, harmful: true);
        }
        else
        {
            SpellUtils.SignalSpell(caster, target, NwSpell.FromSpellId((int)dispelType)!, harmful: false);
        }

        // 2. Calculate dispel caster level
        int clCap = GetCasterLevelCap(dispelType);
        int dispelCl = Math.Min(casterLevel, clCap);
        int featBonus = GetAbjurationFocusBonus(caster);
        dispelCl += featBonus;

        // 3. Begin the dispel process

        // Store an empty list of spells dispelled to send feedback to the caster
        List<(string SpellName, int EffectCasterLevel)> dispelInfo = [];

        // 3.1. First try to dispel spell effects on the target
        List<Effect[]> effectsBySpell = ListEffectsBySpell(target);
        foreach (Effect[] effectsPerSpell in effectsBySpell)
        {
            if (maxSpells > 0 && dispelInfo.Count >= maxSpells) break;

            if (TryDispelEffect(target, effectsPerSpell, dispelCl))
            {
                string spellName = effectsPerSpell[0].Spell?.Name.ToString() ?? "Unknown Spell";
                int effectCasterLevel = effectsPerSpell[0].CasterLevel;
                dispelInfo.Add((spellName, effectCasterLevel));
            }
        }

        // Return early if we've reached the cap before trying to dispel item properties
        if (maxSpells > 0 && dispelInfo.Count >= maxSpells)
        {
            SendDispelFeedback(caster, target, dispelInfo);
            return dispelInfo.Count;
        }

        // 3.2. Then dispel try to dispel temporary item properties on the target
        List<(NwItem Item, ItemProperty[] ItemProperties)> itemPropsByItem = ListItemPropertiesBySpell(target);
        foreach ((NwItem Item, ItemProperty[] ItemProperties) var in itemPropsByItem)
        {
            if (maxSpells > 0 && dispelInfo.Count >= maxSpells) break;

            if (TryDispelItemProperty(var.Item, var.ItemProperties, dispelCl))
            {
                string spellName = var.ItemProperties[0].Spell?.Name.ToString() ?? "Unknown Spell";
                int effectCasterLevel = var.ItemProperties[0].CasterLevel;
                dispelInfo.Add((spellName, effectCasterLevel));
            }
        }

        SendDispelFeedback(caster, target, dispelInfo);
        return dispelInfo.Count;
    }

    /// <summary>
    /// Return a list of all effects on a target, grouped by spell and caster.
    /// Sorted by spell level and caster level from highest to lowest.
    /// </summary>
    private static List<Effect[]> ListEffectsBySpell(NwGameObject target) => target.ActiveEffects
        .Where(e => e is { Spell: not null, SubType: EffectSubType.Magical })
        .GroupBy(e => new {e.Spell, e.Creator})
        .OrderByDescending(group => group.Key.Spell!.InnateSpellLevel)
        .ThenByDescending(group => group.Max(e => e.CasterLevel))
        .Select(group => group.ToArray())
        .ToList();

    /// <summary>
    /// Return a list of all temporary item properties on a target, grouped by item, spell, and caster.
    /// Sorted by spell level and caster level from highest to lowest.
    /// </summary>
    private static List<(NwItem Item, ItemProperty[] ItemProperties)> ListItemPropertiesBySpell(NwGameObject target)
        => GetEquippedItems(target)
            .SelectMany(item => item.ItemProperties, (item, ip) => (Item: item, ItemProperty: ip))
            .Where(x => x.ItemProperty is { Spell: not null, DurationType: EffectDuration.Temporary })
            .GroupBy(x => new { x.Item, x.ItemProperty.Spell, x.ItemProperty.Creator })
            .Select(group => (
                group.Key.Item,
                ItemProperties: group.Select(x => x.ItemProperty).ToArray()
            ))
            .OrderByDescending(x => x.ItemProperties[0].Spell!.InnateSpellLevel)
            .ThenByDescending(x => x.ItemProperties[0].CasterLevel)
            .ToList();

    private static readonly InventorySlot[] AllSlots = Enum.GetValues<InventorySlot>();

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

    /// <summary>
    /// Performs a dispel check against a specific spell effect on a target.
    /// Uses the formula: d20 + dispel caster level >= 12 + effect caster level
    /// If the effect doesn't have a caster level, uses the dispelled creature's HD instead.
    /// </summary>
    /// <param name="target">The target to dispel the effect from</param>
    /// <param name="spellEffects">The effects grouped by spell and caster checked for dispelling</param>
    /// <param name="dispelCl">The dispel caster level, i.e. the strength of the caster's dispel</param>
    /// <returns>True if the effect group is successfully dispelled, otherwise false</returns>
    private static bool TryDispelEffect(NwGameObject target, Effect[] spellEffects, int dispelCl)
    {
        int effectCasterLevel = spellEffects[0].CasterLevel;

        if (!DispelCheck(dispelCl, effectCasterLevel)) return false;

        foreach (Effect effect in spellEffects)
        {
            target.RemoveEffect(effect);
        }

        return true;
    }

    /// <summary>
    /// Performs a dispel check against a specific spell effect on a target.
    /// Uses the formula: d20 + dispel caster level >= 12 + effect caster level
    /// If the effect doesn't have a caster level, uses the dispelled creature's HD instead.
    /// </summary>
    /// <param name="targetItem">The target item to dispel the properties from</param>
    /// <param name="itemProperties">The temporary properties grouped by spell and caster checked for dispelling</param>
    /// <param name="dispelCl">The dispel caster level, i.e. the strength of the caster's dispel</param>
    /// <returns>True if the effect group is successfully dispelled, otherwise false</returns>
    private static bool TryDispelItemProperty(NwItem targetItem, ItemProperty[] itemProperties, int dispelCl)
    {
        int effectCasterLevel = itemProperties[0].CasterLevel;

        if (!DispelCheck(dispelCl, effectCasterLevel)) return false;

        foreach (ItemProperty itemProperty in itemProperties)
        {
            targetItem.RemoveItemProperty(itemProperty);
        }

        return true;
    }

    /// <summary>
    /// Gets the caster level cap for a dispel type.
    /// </summary>
    private static int GetCasterLevelCap(DispelType dispelType) => dispelType switch
    {
        DispelType.LesserDispel => 5,
        DispelType.DispelMagic => 10,
        DispelType.GreaterDispelling => 15,
        DispelType.DevourMagic => 20,
        DispelType.MordenkainensDisjunction => 40,
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
    /// Sends dispel feedback to the caster and target.
    /// </summary>
    private static void SendDispelFeedback(NwCreature caster, NwGameObject target, List<(string, int)> dispelInfo)
    {
        if (dispelInfo.Count == 0) return;

        bool casterIsPlayer = caster.IsPlayerControlled(out NwPlayer? casterPlayer);
        bool targetIsPlayer = target.IsPlayerControlled(out NwPlayer? targetPlayer);
        if (!casterIsPlayer && !targetIsPlayer) return;

        string dispelMessage = $"{caster.Name} dispelled from {target.Name}:".ColorString(ColorConstants.Lime);
        foreach ((string SpellName, int EffectCasterLevel) dispel in dispelInfo)
        {
            dispelMessage += $"\n -{dispel.SpellName} ({dispel.EffectCasterLevel})".ColorString(ColorConstants.Gray);
        }

        casterPlayer?.SendServerMessage(dispelMessage);
        targetPlayer?.SendServerMessage(dispelMessage);
    }

    /// <summary>
    /// Sends dispel feedback to the caster for AOE removal
    /// </summary>
    private static void SendDispelAoeFeedback(NwCreature caster, NwAreaOfEffect areaOfEffect)
    {
        if (!caster.IsPlayerControlled(out NwPlayer? player)) return;

        string spellName = areaOfEffect.Spell?.Name.ToString() ?? "Unknown Spell";
        int effectCasterLevel = areaOfEffect.CasterLevel;

        string dispelMessage = $"Dispelled area of effect: {spellName} ({effectCasterLevel})".ColorString(ColorConstants.Lime);

        player.SendServerMessage(dispelMessage);
    }

    /// <summary>
    /// Performs a dispel check against an Area of Effect object.
    /// </summary>
    /// <param name="caster">The creature casting the dispel</param>
    /// <param name="aoeObject">The AoE object to attempt to dispel</param>
    /// <param name="casterLevel">The effective caster level for the dispel check</param>
    /// <returns>True if the AoE was successfully dispelled</returns>
    public bool TryDispelAreaOfEffect(NwCreature caster, uint aoeObject, int casterLevel)
    {
        // Check if it's a mobile aura (can't dispel these)
        string tag = NWScript.GetTag(aoeObject);
        if (tag.Length >= 7 && tag.Substring(0, 7) == "VFX_MOB")
        {
            return false;
        }

        // Get the AoE creator's caster level
        uint aoeCreator = NWScript.GetAreaOfEffectCreator(aoeObject);
        int aoeCreatorCl = NWScript.GetCasterLevel(aoeCreator);

        // Perform dispel check
        if (DispelCheck(casterLevel, aoeCreatorCl))
        {
            NWScript.DestroyObject(aoeObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs a dispel check against an Area of Effect object.
    /// </summary>
    /// <param name="caster">The creature casting the dispel</param>
    /// <param name="areaOfEffect">The AoE object to attempt to dispel</param>
    /// <param name="casterLevel">The effective caster level for the dispel check</param>
    /// <returns>True if the AoE was successfully dispelled</returns>
    public bool TryDispelAreaOfEffect(NwCreature caster, NwAreaOfEffect areaOfEffect, int casterLevel)
    {
        int dispelCl = casterLevel + GetAbjurationFocusBonus(caster);

        if (areaOfEffect.Tag[..7] == "VFX_MOB")
        {
            return false;
        }

        if (areaOfEffect.Creator == caster || DispelCheck(dispelCl, areaOfEffect.CasterLevel))
        {
            areaOfEffect.Destroy();
            SendDispelAoeFeedback(caster, areaOfEffect);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for conditions that make the target immune to dispel.
    /// </summary>
    /// <param name="targetObject">Target to dispel</param>
    /// <returns>True if the target is immune to dispel, otherwise false</returns>
    public bool IsDispelImmune(NwGameObject targetObject)
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
    /// Performs a dispel check of D20 + Dispel CL vs. 12 + Effect Caster Level
    /// </summary>
    /// <param name="dispelCl">The dispel CL of the dispel caster</param>
    /// <param name="targetEffectCl">The effective CL of the target effect being removed</param>
    /// <returns>True if the dispel check is greater or equal than the dispel resistance, otherwise false</returns>
    private static bool DispelCheck(int dispelCl, int targetEffectCl)
        => Random.Shared.Roll(20) + dispelCl >= 12 + targetEffectCl;
}
