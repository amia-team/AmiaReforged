using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;
using NLog;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(CasterLevelOverrideService))]
public class CasterLevelOverrideService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ShifterDcService _shifterDcService;

    // Dictionary mapping prestige classes to their caster level modifier formulas
    // Formula takes prestige class level and returns the modifier (minimum 0, prevents negative)
    private readonly Dictionary<ClassType, Func<int, int>> _prestigeClassModifiers = new()
    {
        { ClassType.PaleMaster, prcLevel => Math.Max(0, prcLevel - 5) },
        { ClassType.DragonDisciple, prcLevel => Math.Max(0, prcLevel - 5) },
        { ClassType.Blackguard, prcLevel => Math.Max(0, prcLevel - 5) },
        { ClassType.DivineChampion, prcLevel => Math.Max(0, prcLevel - 5) },
        { ClassType.ArcaneArcher, prcLevel => Math.Max(0, prcLevel - 5) }
    };

    private readonly Dictionary<NwCreature, bool> _casterLevelOverridesApplied = new();

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
            new HashSet<ClassType> { ClassType.Cleric, ClassType.Paladin, ClassType.Druid, ClassType.Blackguard }
        }
    };

    public CasterLevelOverrideService(ShifterDcService shifterDcService)
    {
        _shifterDcService = shifterDcService;

        NwModule.Instance.OnClientLeave += RemoveSetup;

        NwModule.Instance.OnLevelUp += FixCasterLevelOnLevelUp;
        NwModule.Instance.OnLevelDown += FixCasterLevelOnLevelDown;
        NwModule.Instance.OnSpellCast += FixCasterLevelOverride;
    }

    /// <summary>
    /// Gets the effective caster level for a creature, accounting for Shifter forms.
    /// Use this method when calculating spell durations or effects for polymorphed Shifters.
    /// </summary>
    /// <param name="creature">The creature to check</param>
    /// <param name="fallbackCasterLevel">Optional fallback (0 means use creature's normal caster level)</param>
    /// <returns>The effective caster level</returns>
    public int GetEffectiveCasterLevel(NwCreature creature, int fallbackCasterLevel = 0)
    {
        return _shifterDcService.GetShifterCasterLevel(creature, fallbackCasterLevel);
    }

    private void RemoveSetup(ModuleEvents.OnClientLeave obj)
    {
        NwCreature? playerLoginCreature = obj.Player.LoginCreature;
        if (playerLoginCreature is null) return;

        _casterLevelOverridesApplied[playerLoginCreature] = false;

        _casterLevelOverridesApplied.Remove(playerLoginCreature);
    }

    private void FixCasterLevelOverride(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.LoginCreature is null) return;
        DoCasterLevelOverride(player.LoginCreature);
    }

    private void FixCasterLevelOnLevelDown(OnLevelDown obj)
    {
        DoCasterLevelOverride(obj.Creature);
    }

    private void FixCasterLevelOnLevelUp(OnLevelUp obj)
    {
        DoCasterLevelOverride(obj.Creature);
    }

    private void FixCasterLevel(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature is null) return;
        DoCasterLevelOverride(obj.Player.LoginCreature);
    }

    private void DoCasterLevelOverride(NwCreature casterCreature)
    {
        // Find all prestige classes that have caster level modifiers
        List<(ClassType classType, int level)> prestigeClasses = [];
        foreach (CreatureClassInfo charClass in casterCreature.Classes)
        {
            if (_prestigeClassModifiers.ContainsKey(charClass.Class.ClassType))
            {
                prestigeClasses.Add((charClass.Class.ClassType, charClass.Level));
            }
        }

        // If no prestige classes with modifiers, no override needed
        if (prestigeClasses.Count == 0) return;

        // Build a map of base class levels for quick lookup
        Dictionary<ClassType, int> baseClassLevels = new();
        foreach (CreatureClassInfo charClass in casterCreature.Classes)
        {
            baseClassLevels[charClass.Class.ClassType] = charClass.Level;
        }

        // Track cumulative modifiers per base class
        Dictionary<ClassType, int> baseClassModifiers = new();

        // Process each prestige class and accumulate modifiers for valid base classes
        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            if (!PrestigeToBaseCasterMap.TryGetValue(prcType, out HashSet<ClassType>? validBaseClasses))
            {
                Log.Warn($"{casterCreature.Name}: No base class mapping found for prestige class {prcType}");
                continue;
            }

            // Find the highest-level valid base class for this prestige class
            ClassType? bestBaseClass = null;
            int bestBaseLevel = 0;
            foreach (ClassType validBase in validBaseClasses)
            {
                if (baseClassLevels.TryGetValue(validBase, out int level) && level > bestBaseLevel)
                {
                    bestBaseLevel = level;
                    bestBaseClass = validBase;
                }
            }

            if (bestBaseClass == null)
            {
                Log.Warn($"{casterCreature.Name}: Prestige class {prcType} has no valid base caster class");
                continue;
            }

            int modifier = _prestigeClassModifiers[prcType](prcLevel);
            Log.Info($"{casterCreature.Name}: Prestige class {prcType} level {prcLevel} adds modifier {modifier} to {bestBaseClass.Value}");

            // Accumulate modifier for this base class
            if (!baseClassModifiers.ContainsKey(bestBaseClass.Value))
            {
                baseClassModifiers[bestBaseClass.Value] = 0;
            }
            baseClassModifiers[bestBaseClass.Value] += modifier;
        }

        // Apply caster level overrides to each affected base class
        foreach ((ClassType baseClass, int totalModifier) in baseClassModifiers)
        {
            int baseLevel = baseClassLevels[baseClass];
            int finalCasterLevel = Math.Max(1, baseLevel + totalModifier);

            Log.Info(
                $"{casterCreature.Name}: Setting caster level override - Base {baseLevel} + Modifier {totalModifier} = {finalCasterLevel} for class {baseClass}");

            CreaturePlugin.SetCasterLevelOverride(casterCreature, (int)baseClass, finalCasterLevel);
        }

        _casterLevelOverridesApplied[casterCreature] = true;
    }
}
