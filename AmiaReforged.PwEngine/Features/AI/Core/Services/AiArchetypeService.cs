using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Detects and manages creature archetypes based on class levels, feats, and equipment.
/// Ports logic from ds_ai_include.nss SetArchetype() (lines 1329-1391).
///
/// Legacy archetypes (5 types):
///   "caster"  (C) — Has usable spell lists; prioritizes casting
///   "melee"   (M) — Default close combat fighter
///   "ranged"  (R) — Has ranged weapon equipped; flee bias modifier
///   "sneak"   (S) — High Hide/Move Silently relative to HD; starts stealthed
///   "hips"    (H) — Has Hide in Plain Sight; 70% chance to disengage and re-stealth
///
/// Dynamic transitions (from legacy):
///   C → M/R  when spell lists depleted (via OnSpellsExhausted)
///   Never M/R → C
/// </summary>
[ServiceBinding(typeof(AiArchetypeService))]
public class AiArchetypeService
{
    private readonly Dictionary<string, IAiArchetype> _archetypes = new();
    private readonly AiStateManager _stateManager;
    private readonly bool _isEnabled;

    /// <summary>
    /// Percentage chance (0-100) that a HiPS creature will disengage and hide.
    /// Matches legacy HIPS_CHANCE constant.
    /// </summary>
    public const int HipsChance = 70;

    public AiArchetypeService(
        IEnumerable<IAiArchetype> archetypes,
        AiStateManager stateManager)
    {
        _stateManager = stateManager;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";

        if (!_isEnabled) return;

        foreach (IAiArchetype archetype in archetypes)
        {
            _archetypes[archetype.ArchetypeId] = archetype;
        }
    }

    /// <summary>
    /// Gets the archetype for a creature, detecting it if not already assigned.
    /// </summary>
    public IAiArchetype? GetArchetype(NwCreature creature)
    {
        if (!_isEnabled) return null;

        AiState state = _stateManager.GetOrCreateState(creature);

        if (!string.IsNullOrEmpty(state.ArchetypeId))
        {
            return _archetypes.GetValueOrDefault(state.ArchetypeId);
        }

        string archetypeId = DetectFullArchetype(creature, state);
        state.ArchetypeId = archetypeId;
        state.ArchetypeValue = GetArchetypeNumericValue(creature);
        return _archetypes.GetValueOrDefault(archetypeId);
    }

    /// <summary>
    /// Gets the numeric archetype value (1-10) for a creature.
    /// Used by combat logic for flee bias, spell selection weight, etc.
    /// </summary>
    public int GetArchetypeValue(NwCreature creature)
    {
        if (!_isEnabled) return 5;

        AiState state = _stateManager.GetOrCreateState(creature);
        if (state.ArchetypeValue > 0) return state.ArchetypeValue;

        state.ArchetypeValue = GetArchetypeNumericValue(creature);
        return state.ArchetypeValue;
    }

    /// <summary>
    /// Called when a creature's spell lists are exhausted.
    /// Transitions caster → melee/ranged based on equipped weapon.
    /// Ports the RemoveSpellList → SetArchetype flow from ds_ai_include.nss.
    /// </summary>
    public void OnSpellsExhausted(NwCreature creature)
    {
        if (!_isEnabled) return;

        AiState state = _stateManager.GetOrCreateState(creature);

        // Only casters transition (legacy: C → M/R, never M/R → C)
        if (state.ArchetypeId != "caster") return;

        string newArchetype = HasRangedWeaponEquipped(creature) ? "ranged" : "melee";
        state.ArchetypeId = newArchetype;
        state.ArchetypeValue = GetArchetypeNumericValue(creature);
    }

    /// <summary>
    /// Checks if a creature has the HiPS archetype.
    /// </summary>
    public bool IsHipsArchetype(NwCreature creature)
    {
        AiState state = _stateManager.GetOrCreateState(creature);
        return state.ArchetypeId == "hips";
    }

    /// <summary>
    /// Checks if a creature has the sneak archetype.
    /// </summary>
    public bool IsSneakArchetype(NwCreature creature)
    {
        AiState state = _stateManager.GetOrCreateState(creature);
        return state.ArchetypeId == "sneak";
    }

    /// <summary>
    /// Checks if a creature has the ranged archetype.
    /// </summary>
    public bool IsRangedArchetype(NwCreature creature)
    {
        AiState state = _stateManager.GetOrCreateState(creature);
        return state.ArchetypeId == "ranged";
    }

    /// <summary>
    /// Checks if a creature is a caster archetype (including hybrids with spell access).
    /// </summary>
    public bool IsCasterArchetype(NwCreature creature)
    {
        AiState state = _stateManager.GetOrCreateState(creature);
        return state.ArchetypeId == "caster";
    }

    /// <summary>
    /// Full archetype detection matching legacy SetArchetype() priority:
    /// 1. Has spell lists → "caster" (C)
    /// 2. Has HiPS feat → "hips" (H)
    /// 3. High Hide/Move Silently → "sneak" (S)
    /// 4. Ranged weapon equipped → "ranged" (R)
    /// 5. Default → "melee" (M)
    /// </summary>
    private string DetectFullArchetype(NwCreature creature, AiState state)
    {
        // Priority 1: Has usable spells → caster
        if (HasUsableSpells(creature))
        {
            return "caster";
        }

        // Priority 2: Has Hide in Plain Sight feat → hips
        if (creature.KnowsFeat(Feat.HideInPlainSight))
        {
            return "hips";
        }

        // Priority 3: High stealth skills relative to HD → sneak
        // Legacy: Hide > HD && Move Silently > HD (first-run only)
        int hitDice = creature.Level;
        int hideSkill = creature.GetSkillRank(Skill.Hide);
        int moveSilentlySkill = creature.GetSkillRank(Skill.MoveSilently);

        if (hideSkill > hitDice && moveSilentlySkill > hitDice)
        {
            return "sneak";
        }

        // Priority 4: Ranged weapon equipped → ranged
        if (HasRangedWeaponEquipped(creature))
        {
            return "ranged";
        }

        // Priority 5: Default → melee
        return "melee";
    }

    /// <summary>
    /// Gets the numeric archetype value (1-10) based on class levels.
    /// Used for flee bias and spell selection weighting.
    /// Port of the class-weighted formula from ds_ai_include.nss.
    /// </summary>
    private int GetArchetypeNumericValue(NwCreature creature)
    {
        int totalLevels = 0;
        int weightedLevels = 0;

        for (int i = 0; i < 3; i++)
        {
            CreatureClassInfo? classInfo = creature.Classes.ElementAtOrDefault(i);
            if (classInfo == null) break;

            int levels = classInfo.Level;
            int factor = GetClassWeightingFactor(classInfo.Class.ClassType);

            totalLevels += levels;
            weightedLevels += levels * factor;
        }

        if (totalLevels == 0) return 1;

        float ratio = (float)weightedLevels / totalLevels;
        int archetype = (int)Math.Ceiling(ratio * 10.0f / 3.0f);
        return Math.Clamp(archetype, 1, 10);
    }

    /// <summary>
    /// Checks if a creature has any usable spell lists.
    /// Matches legacy check: spell list is not empty, not "------", not heal-only "--h---".
    /// </summary>
    private bool HasUsableSpells(NwCreature creature)
    {
        // Scan a subset of spells to check if creature has offensive/buff spells
        for (int i = 0; i < 803; i++)
        {
            NwSpell? spell = NwSpell.FromSpellId(i);
            if (spell != null && creature.HasSpellUse(spell))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the creature has a ranged weapon in its right hand.
    /// </summary>
    private bool HasRangedWeaponEquipped(NwCreature creature)
    {
        NwItem? rightHand = creature.GetItemInSlot(InventorySlot.RightHand);
        if (rightHand == null) return false;

        BaseItemType baseType = rightHand.BaseItem.ItemType;
        return baseType == BaseItemType.Longbow ||
               baseType == BaseItemType.Shortbow ||
               baseType == BaseItemType.LightCrossbow ||
               baseType == BaseItemType.HeavyCrossbow ||
               baseType == BaseItemType.Sling ||
               baseType == BaseItemType.ThrowingAxe ||
               baseType == BaseItemType.Dart ||
               baseType == BaseItemType.Shuriken;
    }

    /// <summary>
    /// Gets the class weighting factor for archetype calculation.
    /// Port of GetClassFactor() from ds_ai_include.nss lines 1368-1391.
    /// </summary>
    private int GetClassWeightingFactor(ClassType classType)
    {
        return classType switch
        {
            // Pure martial classes (factor 1)
            ClassType.Fighter => 1,
            ClassType.Barbarian => 1,
            ClassType.Ranger => 1,
            ClassType.Rogue => 1,

            // Hybrid classes (factor 2)
            ClassType.Paladin => 2,
            ClassType.Monk => 2,
            ClassType.Druid => 2,
            ClassType.Cleric => 2,

            // Full caster classes (factor 3)
            ClassType.Wizard => 3,
            ClassType.Sorcerer => 3,
            ClassType.Bard => 3,

            // Prestige/other classes default to martial
            _ => 1
        };
    }
}
