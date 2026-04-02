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
    private record DispelInfo
    (
        string Name,
        int InnateLevel,
        int CasterLevel,
        Func<bool> DispelAction
    );

    /// <summary>
    /// Dispels all or a set number of spells on a target using Amia's custom dispel system.
    /// Runs dispel checks per magical effects and temporary item properties, grouped by spell and creator.
    /// Starts from the highest spell level and caster level, working down to the lowest.
    /// Prints a list of dispelled spells in the combat log to the caster and target.
    /// </summary>
    /// <param name="caster">The creature casting the dispel</param>
    /// <param name="target">The target to dispel effects from</param>
    /// <param name="dispelModifier">The effective dispel caster level for dispel checks, use GetDispelModifier</param>
    /// <param name="maxSpells">Maximum number of spells to dispel (0 = unlimited)</param>
    /// <returns>The number of spells successfully dispelled</returns>
    public int DispelTarget(NwCreature caster, NwGameObject target, int dispelModifier, int maxSpells = 0)
    {
        // 3. Begin the dispel process: Iterate dispels into a dispel info list
        List<DispelInfo> spellsToDispel = [];

        // 3.1. Map magic effects to dispel info
        spellsToDispel.AddRange(GetEffectsBySpell(target).Select(effects => new DispelInfo
        (
            Name: effects[0].Spell?.Name.ToString() ?? "Unknown Spell",
            InnateLevel: effects[0].Spell?.InnateSpellLevel ?? 0,
            CasterLevel: effects[0].CasterLevel,
            DispelAction: () => TryDispelEffect(target, effects, dispelModifier)
        )));

        // 3.2. Map pairs of item and item property to dispel info
        spellsToDispel.AddRange(GetItemPropertiesBySpell(target).Select(pair => new DispelInfo
        (
            Name: pair.ItemProperties[0].Spell?.Name.ToString() ?? "Unknown Spell",
            InnateLevel: pair.ItemProperties[0].Spell?.InnateSpellLevel ?? 0,
            CasterLevel: pair.ItemProperties[0].CasterLevel,
            DispelAction: () => TryDispelItemProperty(pair.Item, pair.ItemProperties, dispelModifier)
        )));

        // 3.3. Sort dispel info by spell level and caster level so dispelling prioritizes strongest effects
        spellsToDispel = spellsToDispel
            .OrderByDescending(t => t.InnateLevel)
            .ThenByDescending(t => t.CasterLevel)
            .ToList();

        List<(string SpellName, int EffectCasterLevel)> dispelFeedbackList = [];

        // 4. Dispel spells in dispel info list
        foreach (DispelInfo dispelInfo in spellsToDispel)
        {
            // If a max number of spells is specified, stop dispelling after that number is dispelled
            if (maxSpells > 0 && spellsToDispel.Count >= maxSpells) break;

            if (dispelInfo.DispelAction.Invoke())
            {
                dispelFeedbackList.Add((dispelInfo.Name, dispelInfo.CasterLevel));
            }
        }

        // 5. Send dispel feedback to the caster and target, removing duplicate feedback info
        dispelFeedbackList = dispelFeedbackList.Distinct().ToList();
        SendDispelFeedback(caster, target, dispelFeedbackList);
        return spellsToDispel.Count;
    }

    /// <summary>
    /// Return a list of all effects on a target, grouped by spell and caster.
    /// </summary>
    private static List<Effect[]> GetEffectsBySpell(NwGameObject target) => target.ActiveEffects
        .Where(e => e is { Spell: not null, SubType: EffectSubType.Magical,
            EffectType: not (EffectType.SummonCreature or EffectType.Swarm) })
        .GroupBy(e => new {e.Spell, e.Creator})
        .Select(group => group.ToArray())
        .ToList();

    /// <summary>
    /// Return a list of all temporary item properties on a target, grouped by item, spell, and caster.
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
    /// If the effect doesn't have a caster level, uses the dispelled creature's HD instead.
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
    /// Gets the dispel modifier for the dispel type, used for dispel checks.
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


    private const Spell DevourMagic = (Spell)1014;
    /// <summary>
    /// Gets the caster level cap for a dispel type.
    /// </summary>
    private static int GetCasterLevelCap(Spell spell) => spell switch
    {
        Spell.LesserDispel => 5,
        Spell.DispelMagic => 10,
        Spell.GreaterDispelling => 15,
        DevourMagic => 20,
        Spell.MordenkainensDisjunction => 40,
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

        string dispelMessage = $"{caster.Name} dispelled from {target.Name}:".ColorString(ColorConstants.Magenta);
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

        string dispelMessage = $"Dispelled area of effect: {spellName} ({effectCasterLevel})".ColorString(ColorConstants.Magenta);

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
        if (RollDispelCheck(casterLevel, aoeCreatorCl))
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
        int dispelModifier = casterLevel + GetAbjurationFocusBonus(caster);

        if (areaOfEffect.Tag[..7] == "VFX_MOB")
        {
            return false;
        }

        if (areaOfEffect.Creator == caster || RollDispelCheck(dispelModifier, areaOfEffect.CasterLevel))
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
    /// Rolls a dispel check of D20 + Dispel CL vs. 12 + Effect Caster Level
    /// </summary>
    /// <param name="dispelModifier">The caster's dispel modifier</param>
    /// <param name="targetEffectCl">The effective CL of the target effect being removed</param>
    /// <returns>True if the dispel check is greater or equal than the dispel resistance, otherwise false</returns>
    private static bool RollDispelCheck(int dispelModifier, int targetEffectCl)
        => Random.Shared.Roll(20) + dispelModifier >= 12 + targetEffectCl;
}
