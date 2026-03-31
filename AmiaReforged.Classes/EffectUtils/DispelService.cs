using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.EffectUtils;

/// <summary>
/// Service for handling dispel magic mechanics with custom logic matching Amia's dispel system.
/// Provides per-effect dispel checks, PvP bonuses, and detailed feedback to players.
/// </summary>
[ServiceBinding(typeof(DispelService))]
public class DispelService
{
    /// <summary>
    /// Dispel type identifiers matching NWN spell constants.
    /// </summary>
    public enum DispelType
    {
        LesserDispel = 165,      // SPELL_LESSER_DISPEL
        DispelMagic = 41,        // SPELL_DISPEL_MAGIC
        GreaterDispelling = 67,  // SPELL_GREATER_DISPELLING
        MordenkainensDisjunction = 112, // SPELL_MORDENKAINENS_DISJUNCTION
        DevourMagic = 1014
    }

    private const int DispelCooldownSeconds = 30;
    private const string DispelTimerVar = "dispel_timer";

    /// <summary>
    /// Dispels effects on a target using Amia's custom dispel system.
    /// Iterates through magical effects and performs individual dispel checks with feedback.
    /// </summary>
    /// <param name="caster">The creature casting the dispel</param>
    /// <param name="target">The target to dispel effects from</param>
    /// <param name="casterLevel">The effective caster level for dispel checks</param>
    /// <param name="dispelType">The type of dispel spell being used</param>
    /// <param name="maxSpells">Maximum number of spells to dispel (0 = unlimited)</param>
    /// <returns>The number of effects successfully dispelled</returns>
    public int DispelEffectsAll(NwCreature caster, NwGameObject target, int casterLevel, DispelType dispelType, int maxSpells = 0)
    {
        uint casterOid = caster.ObjectId;
        uint targetOid = target.ObjectId;

        // Signal spell cast event
        if (target is NwCreature targetCreature && caster.IsReactionTypeHostile(targetCreature))
        {
            SpellUtils.SignalSpell(caster, targetCreature, NwSpell.FromSpellId((int)dispelType)!, harmful: true);;
        }
        else
        {
            SpellUtils.SignalSpell(caster, target, NwSpell.FromSpellId((int)dispelType)!, harmful: false);
        }

        // Apply CL cap based on dispel type
        int clCap = GetCasterLevelCap(dispelType);
        int dispelCl = Math.Min(casterLevel, clCap);

        // Calculate feat bonuses (+2 per abjuration focus feat)
        int featBonus = GetAbjurationFocusBonus(caster);
        dispelCl += featBonus;

        int dispelledCount = 0;
        HashSet<int> processedSpells = [];

        // Iterate through all effects on target
        IntPtr currentEffect = GetFirstEffect(targetOid);
        while (GetIsEffectValid(currentEffect) == TRUE)
        {
            int spellId = GetEffectSpellId(currentEffect);

            // Only process magical effects with valid spell IDs that we haven't processed yet
            if (spellId > -1 && GetEffectSubType(currentEffect) == SUBTYPE_MAGICAL && processedSpells.Add(spellId))
            {
                // Perform dispel check
                if (TryDispelSpell(caster, target, spellId, dispelCl))
                {
                    string spellName = GetSpellName(spellId);
                    SendDispelFeedback(caster, target, spellName, dispelCl);
                    RemoveItemEnchantments(target, spellId);

                    dispelledCount++;

                    if (maxSpells > 0 && dispelledCount >= maxSpells)
                    {
                        return dispelledCount;
                    }
                }
            }

            currentEffect = GetNextEffect(targetOid);
        }

        return dispelledCount;
    }

    /// <summary>
    /// Performs a dispel check against a specific spell effect on a target.
    /// Uses the formula: d20 + casterCL >= 12 + targetHitDice
    /// </summary>
    private bool TryDispelSpell(NwCreature caster, NwGameObject target, int spellId, int dispelCl)
    {
        uint targetOid = target.ObjectId;

        int targetHitDice = target is NwCreature creature ? creature.Level : GetHitDice(targetOid);
        int enemyBase = 12;

        int casterRoll = d20() + dispelCl;
        int enemyRoll = enemyBase + targetHitDice;

        // On tie, dispel succeeds
        if (casterRoll >= enemyRoll)
        {
            // Remove all effects from this spell
            IntPtr effect = GetFirstEffect(targetOid);
            while (GetIsEffectValid(effect) == TRUE)
            {
                if (GetEffectSpellId(effect) == spellId)
                {
                    RemoveEffect(targetOid, effect);
                }
                effect = GetNextEffect(targetOid);
            }
            return true;
        }

        return false;
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
    /// Gets the display name for a spell.
    /// </summary>
    private static string GetSpellName(int spellId)
    {
        string strRef = Get2DAString("spells", "Name", spellId);
        if (int.TryParse(strRef, out int strRefInt))
        {
            return GetStringByStrRef(strRefInt);
        }
        return $"Spell #{spellId}";
    }

    /// <summary>
    /// Sends dispel feedback to the caster and target.
    /// </summary>
    private static void SendDispelFeedback(NwCreature caster, NwGameObject target, string spellName, int dispelCl)
    {
        NwPlayer? casterPlayer = caster.ControllingPlayer;
        string targetName = target.Name;

        if (caster.ObjectId == target.ObjectId)
        {
            casterPlayer?.SendServerMessage($"Self Dispelled: {spellName} ({dispelCl})".ColorString(ColorConstants.Lime));
        }
        else
        {
            casterPlayer?.SendServerMessage($"Dispelled {targetName}: {spellName}".ColorString(ColorConstants.Lime));

            if (target is NwCreature targetCreature)
            {
                NwPlayer? targetPlayer = targetCreature.ControllingPlayer;
                targetPlayer?.SendServerMessage($"Dispelled {targetName}: {spellName} ({dispelCl})".ColorString(ColorConstants.Lime));
            }
        }
    }

    /// <summary>
    /// Removes temporary item enchantments from a target when a buff spell is dispelled.
    /// </summary>
    private static void RemoveItemEnchantments(NwGameObject target, int spellId)
    {
        if (target is not NwCreature creature) return;

        // Check equipped items for temporary properties tied to this spell
        foreach (NwItem? item in creature.Inventory.Items.Concat(creature.Inventory.Items))
        {
            if (item == null) continue;

            // Use NWScript to iterate item properties since we need to check duration type
            uint itemOid = item.ObjectId;
            IntPtr itemProp = GetFirstItemProperty(itemOid);

            while (GetIsItemPropertyValid(itemProp) == TRUE)
            {
                if (GetItemPropertyDurationType(itemProp) == DURATION_TYPE_TEMPORARY)
                {
                    RemoveItemProperty(itemOid, itemProp);
                }
                itemProp = GetNextItemProperty(itemOid);
            }
        }
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
        string tag = GetTag(aoeObject);
        if (tag.Length >= 7 && tag.Substring(0, 7) == "VFX_MOB")
        {
            return false;
        }

        // Get the AoE creator's caster level
        uint aoeCreator = GetAreaOfEffectCreator(aoeObject);
        int aoeCreatorCl = GetCasterLevel(aoeCreator);

        // Perform dispel check
        if (DispelCheck(casterLevel, aoeCreatorCl))
        {
            DestroyObject(aoeObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs a dispel check against an Area of Effect object.
    /// </summary>
    /// <param name="caster">The creature casting the dispel</param>
    /// <param name="aoeObject">The AoE object to attempt to dispel</param>
    /// <param name="casterLevel">The effective caster level for the dispel check</param>
    /// <returns>True if the AoE was successfully dispelled</returns>
    public bool TryDispelAreaOfEffect(NwCreature caster, NwAreaOfEffect aoeObject, int casterLevel)
    {
        // Check if it's a mobile aura (can't dispel these)
        if (aoeObject.Tag[..7] == "VFX_MOB")
        {
            return false;
        }

        if (aoeObject.Creator == caster || DispelCheck(dispelCl: casterLevel, targetEffectCl: aoeObject.CasterLevel))
        {
            aoeObject.Destroy();
            SendDispelFeedback(caster, aoeObject, aoeObject.Spell?.Name.ToString() ?? "unknown spell", casterLevel);
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
    /// Performs a dispel check of D20 + Dispel CL vs. 11 + Effect CL.
    /// </summary>
    /// <param name="dispelCl">The dispel CL of the dispel caster</param>
    /// <param name="targetEffectCl">The effective CL of the target effect being removed</param>
    /// <returns>True if the dispel check is greater or equal than the opposing check, otherwise false</returns>
    private static bool DispelCheck(int dispelCl, int targetEffectCl)
        => d20() + dispelCl >= 11 + targetEffectCl;

}
