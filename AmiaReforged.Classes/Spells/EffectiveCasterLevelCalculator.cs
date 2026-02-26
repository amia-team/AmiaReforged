using Anvil.API;
using NLog;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Shared helper for calculating effective caster level with prestige class stacking.
/// Used by CasterLevelOverrideService, InfiniteCantripService, SpellLearningService, and DivineCasterSpellAccessService.
///
/// Rules:
/// - Prestige class levels that stack onto a base class are combined first
/// - A single -5 penalty is applied to the combined total (not per prestige class)
/// - True base caster classes are prioritized over prestige classes as targets
/// - Blackguard can act as a fallback target for Divine Champion if no true divine base exists
/// </summary>
public static class EffectiveCasterLevelCalculator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Prestige classes that contribute to caster level stacking
    // Their levels are combined, then -5 is applied once to the total
    private static readonly HashSet<ClassType> PrestigeClassesWithCLBonus = new()
    {
        ClassType.PaleMaster,
        ClassType.DragonDisciple,
        ClassType.Blackguard,
        ClassType.DivineChampion,
        ClassType.ArcaneArcher
    };

    // "True" base caster classes - these always receive the CL bump, never prestige classes
    private static readonly HashSet<ClassType> TrueBaseCasterClasses = new()
    {
        ClassType.Wizard, ClassType.Sorcerer, ClassType.Bard,  // Arcane
        ClassType.Cleric, ClassType.Druid, ClassType.Ranger, ClassType.Paladin,  // Divine
        ClassType.Assassin  // Treated as base for PM/AA (doesn't get modified itself)
    };

    // Mapping of prestige classes to their valid base caster classes
    private static readonly Dictionary<ClassType, HashSet<ClassType>> PrestigeToBaseCasterMap = new()
    {
        {
            ClassType.PaleMaster,
            new HashSet<ClassType> { ClassType.Wizard, ClassType.Sorcerer, ClassType.Bard, ClassType.Assassin }
        },
        {
            ClassType.DragonDisciple,
            new HashSet<ClassType> { ClassType.Sorcerer, ClassType.Bard }
        },
        {
            ClassType.ArcaneArcher,
            new HashSet<ClassType> { ClassType.Wizard, ClassType.Sorcerer, ClassType.Bard, ClassType.Assassin }
        },
        {
            ClassType.Blackguard,
            new HashSet<ClassType> { ClassType.Cleric, ClassType.Druid, ClassType.Ranger }
        },
        {
            ClassType.DivineChampion,
            // Divine Champion can stack onto true divine base classes, OR onto Blackguard if no true base exists
            new HashSet<ClassType> { ClassType.Cleric, ClassType.Paladin, ClassType.Druid, ClassType.Blackguard }
        }
    };

    /// <summary>
    /// Calculates effective caster levels for all applicable classes on a creature.
    /// Returns a dictionary mapping each target class to its effective caster level.
    /// </summary>
    public static Dictionary<ClassType, int> CalculateAllEffectiveCasterLevels(NwCreature creature)
    {
        Dictionary<ClassType, int> result = new();

        // Build a map of all class levels for quick lookup
        Dictionary<ClassType, int> classLevels = new();
        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            classLevels[charClass.Class.ClassType] = charClass.Level;
        }

        // Find all prestige classes that have caster level modifiers
        List<(ClassType classType, int level)> prestigeClasses = new();
        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            if (PrestigeClassesWithCLBonus.Contains(charClass.Class.ClassType))
            {
                prestigeClasses.Add((charClass.Class.ClassType, charClass.Level));
            }
        }

        // If no prestige classes, just return base class levels for caster classes
        if (prestigeClasses.Count == 0)
        {
            foreach (var kvp in classLevels)
            {
                if (TrueBaseCasterClasses.Contains(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }

        // Group prestige classes by their target base class, accumulating total PRC levels
        // Key: target class, Value: (base level, total PRC levels stacking onto it)
        Dictionary<ClassType, (int baseLevel, int totalPrcLevels)> targetClassData = new();

        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            if (!PrestigeToBaseCasterMap.TryGetValue(prcType, out HashSet<ClassType>? validBaseClasses))
            {
                continue;
            }

            // Find the best target class for this prestige class's bonus
            // Priority: True base caster classes first, then fallback to prestige classes (like BG for DC)
            ClassType? targetClass = null;
            int targetClassLevel = 0;
            bool targetIsTrueBase = false;

            foreach (ClassType validBase in validBaseClasses)
            {
                if (!classLevels.TryGetValue(validBase, out int level)) continue;

                bool isTrueBase = TrueBaseCasterClasses.Contains(validBase);

                // Prefer true base classes over prestige classes
                // If both are same type (both true or both prestige), pick higher level
                if (targetClass == null ||
                    (isTrueBase && !targetIsTrueBase) ||  // True base beats prestige
                    (isTrueBase == targetIsTrueBase && level > targetClassLevel))  // Same type, higher level wins
                {
                    targetClass = validBase;
                    targetClassLevel = level;
                    targetIsTrueBase = isTrueBase;
                }
            }

            if (targetClass == null)
            {
                continue;
            }

            // Accumulate PRC levels for this target class
            if (!targetClassData.ContainsKey(targetClass.Value))
            {
                targetClassData[targetClass.Value] = (targetClassLevel, prcLevel);
            }
            else
            {
                var existing = targetClassData[targetClass.Value];
                targetClassData[targetClass.Value] = (existing.baseLevel, existing.totalPrcLevels + prcLevel);
            }
        }

        // Calculate effective CL for each target class: base level + (total PRC levels - 5)
        // The -5 penalty is applied ONCE to the combined PRC levels
        foreach ((ClassType targetClass, (int baseLevel, int totalPrcLevels)) in targetClassData)
        {
            int prcBonus = Math.Max(0, totalPrcLevels - 5);
            int effectiveLevel = baseLevel + prcBonus;
            result[targetClass] = effectiveLevel;
        }

        // Also include base caster classes without prestige bonuses (at their actual level)
        foreach (var kvp in classLevels)
        {
            if (TrueBaseCasterClasses.Contains(kvp.Key) && !result.ContainsKey(kvp.Key))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the highest effective caster level across all classes for a creature.
    /// </summary>
    public static int GetHighestEffectiveCasterLevel(NwCreature creature)
    {
        var allLevels = CalculateAllEffectiveCasterLevels(creature);
        return allLevels.Count > 0 ? allLevels.Values.Max() : 0;
    }

    /// <summary>
    /// Gets the effective caster level for a specific class.
    /// Returns 0 if the creature doesn't have that class or it's not a caster class.
    /// </summary>
    public static int GetEffectiveCasterLevelForClass(NwCreature creature, ClassType classType)
    {
        var allLevels = CalculateAllEffectiveCasterLevels(creature);
        return allLevels.TryGetValue(classType, out int level) ? level : 0;
    }

    /// <summary>
    /// Checks if a class type is a prestige class that contributes to CL stacking.
    /// </summary>
    public static bool IsPrestigeClassWithCLBonus(ClassType classType) =>
        PrestigeClassesWithCLBonus.Contains(classType);

    /// <summary>
    /// Checks if a class type is a true base caster class.
    /// </summary>
    public static bool IsTrueBaseCasterClass(ClassType classType) =>
        TrueBaseCasterClasses.Contains(classType);

    /// <summary>
    /// Gets the set of valid base classes for a prestige class.
    /// </summary>
    public static HashSet<ClassType>? GetValidBaseClassesForPrestige(ClassType prestigeClass) =>
        PrestigeToBaseCasterMap.TryGetValue(prestigeClass, out var result) ? result : null;

    /// <summary>
    /// Gets all prestige classes that contribute to CL stacking.
    /// </summary>
    public static IReadOnlySet<ClassType> GetPrestigeClassesWithCLBonus() => PrestigeClassesWithCLBonus;

    /// <summary>
    /// Gets all true base caster classes.
    /// </summary>
    public static IReadOnlySet<ClassType> GetTrueBaseCasterClasses() => TrueBaseCasterClasses;
}

