using Anvil.API;
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
        MordenkainensDisjunction = 112 // SPELL_MORDENKAINENS_DISJUNCTION
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
        if (IsHostileTarget(target, caster))
        {
            SignalEvent(targetOid, EventSpellCastAt(casterOid, (int)dispelType));
        }
        else
        {
            SignalEvent(targetOid, EventSpellCastAt(casterOid, (int)dispelType, FALSE));
        }

        // Apply CL cap based on dispel type
        int clCap = GetCasterLevelCap(dispelType);
        int effectiveCl = Math.Min(casterLevel, clCap);

        // Calculate feat bonuses (+2 per abjuration focus feat)
        int featBonus = CalculateFeatBonus(caster);
        effectiveCl += featBonus;

        // PvP detection
        bool isPvP = IsPvPDispel(caster, target);

        // Bonus system setup
        var bonusConfig = GetBonusConfiguration(dispelType, isPvP);
        int bonusCounter = bonusConfig.BonusCounter;
        bool canGetBonus = !bonusConfig.IsDisqualified && CheckDispelCooldown(target);

        int dispelledCount = 0;
        HashSet<int> processedSpells = new();

        // Iterate through all effects on target
        IntPtr currentEffect = GetFirstEffect(targetOid);
        while (GetIsEffectValid(currentEffect) == TRUE)
        {
            int spellId = GetEffectSpellId(currentEffect);

            // Only process magical effects with valid spell IDs that we haven't processed yet
            if (spellId > -1 && GetEffectSubType(currentEffect) == SUBTYPE_MAGICAL && !processedSpells.Contains(spellId))
            {
                processedSpells.Add(spellId);

                // Calculate multiplier from bonus system
                int multiplier = 1;
                if (canGetBonus && bonusCounter > 0)
                {
                    multiplier = CalculateBonusMultiplier(bonusConfig, isPvP, caster, target);
                    if (multiplier > 1)
                    {
                        bonusCounter--;
                    }
                }

                int dispelCl = (effectiveCl * multiplier);

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
        DispelType.MordenkainensDisjunction => 40,
        _ => 10
    };

    /// <summary>
    /// Calculates feat bonus for abjuration focus (+2 per tier).
    /// </summary>
    private static int CalculateFeatBonus(NwCreature caster)
    {
        int bonus = 0;

        if (caster.KnowsFeat(NwFeat.FromFeatId(35)!)) // Spell Focus: Abjuration
            bonus += 2;
        if (caster.KnowsFeat(NwFeat.FromFeatId(393)!)) // Greater Spell Focus: Abjuration
            bonus += 2;
        if (caster.KnowsFeat(NwFeat.FromFeatId(610)!)) // Epic Spell Focus: Abjuration
            bonus += 2;

        return bonus;
    }

    /// <summary>
    /// Determines if this is a PvP dispel scenario.
    /// </summary>
    private static bool IsPvPDispel(NwCreature caster, NwGameObject target)
    {
        bool casterIsPlayer = caster.IsPlayerControlled || caster.IsPossessedFamiliar;

        if (target is NwCreature targetCreature)
        {
            bool targetIsPlayer = targetCreature.IsPlayerControlled || targetCreature.IsPossessedFamiliar;
            return casterIsPlayer && targetIsPlayer;
        }

        return false;
    }

    /// <summary>
    /// Checks if the target is hostile to the caster.
    /// </summary>
    private static bool IsHostileTarget(NwGameObject target, NwCreature caster)
    {
        if (target is NwCreature targetCreature)
        {
            return caster.IsReactionTypeHostile(targetCreature);
        }
        return false;
    }

    /// <summary>
    /// Configuration for the bonus dispel system.
    /// </summary>
    private record BonusConfiguration(int BonusMod, int BonusCounter, bool IsDisqualified);

    /// <summary>
    /// Gets bonus configuration based on dispel type and PvP status.
    /// </summary>
    private static BonusConfiguration GetBonusConfiguration(DispelType dispelType, bool isPvP)
    {
        var config = dispelType switch
        {
            DispelType.LesserDispel => new BonusConfiguration(25, 1, false),
            DispelType.DispelMagic => new BonusConfiguration(50, 2, false),
            DispelType.GreaterDispelling => new BonusConfiguration(100, 3, false),
            DispelType.MordenkainensDisjunction => new BonusConfiguration(0, 0, true),
            _ => new BonusConfiguration(50, 2, false)
        };

        // PvP always gets full bonus chance
        if (isPvP)
        {
            return config with { BonusMod = 100 };
        }

        return config;
    }

    /// <summary>
    /// Checks dispel cooldown and updates timer if needed.
    /// </summary>
    private static bool CheckDispelCooldown(NwGameObject target)
    {
        uint targetOid = target.ObjectId;
        int lastDispelTime = GetLocalInt(targetOid, DispelTimerVar);
        int currentTime = (int)(DateTimeOffset.Now.ToUnixTimeSeconds() % int.MaxValue);

        if (lastDispelTime > 0)
        {
            if (lastDispelTime + DispelCooldownSeconds < currentTime)
            {
                SetLocalInt(targetOid, DispelTimerVar, currentTime);
                return true;
            }
            return false;
        }

        SetLocalInt(targetOid, DispelTimerVar, currentTime);
        return true;
    }

    /// <summary>
    /// Calculates the bonus multiplier for a dispel check.
    /// </summary>
    private static int CalculateBonusMultiplier(BonusConfiguration config, bool isPvP, NwCreature caster, NwGameObject target)
    {
        const int occurrencePercent = 10;
        const int pvpPenalty = 25;

        int bonusRoll = Random(100);
        int randomCheck = Random(100);

        if (config.BonusMod < bonusRoll || occurrencePercent < randomCheck)
        {
            return 1;
        }

        // Calculate caster benefit based on level difference
        int casterBenefit = 0;
        if (target is NwCreature targetCreature)
        {
            int levelDiff = caster.Level - targetCreature.Level;
            casterBenefit = Math.Clamp(levelDiff, 0, 5);
        }

        int bonusDiceRoll = bonusRoll - casterBenefit;

        if (isPvP)
        {
            bonusDiceRoll -= pvpPenalty;
            if (bonusDiceRoll <= 0) bonusDiceRoll = 1;
        }

        return bonusDiceRoll switch
        {
            >= 1 and <= 5 => 4,
            >= 6 and <= 20 => 3,
            >= 21 and <= 50 => 2,
            _ => 1
        };
    }

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
        if (NwEffects.DispelCheck(casterLevel, aoeCreatorCl) == TRUE)
        {
            DestroyObject(aoeObject);
            return true;
        }

        return false;
    }
}
