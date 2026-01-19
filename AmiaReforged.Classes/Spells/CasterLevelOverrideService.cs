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

    // Base caster classes that can receive prestige class bonuses
    private static readonly HashSet<ClassType> BaseCasterClasses = new()
    {
        ClassType.Wizard,
        ClassType.Sorcerer,
        ClassType.Bard,
        ClassType.Assassin,
        ClassType.Druid,
        ClassType.Cleric,
        ClassType.Paladin
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

        // Find all base caster classes
        List<(int level, int classConst)> baseClasses = [];
        foreach (CreatureClassInfo charClass in casterCreature.Classes)
        {
            if (BaseCasterClasses.Contains(charClass.Class.ClassType))
            {
                baseClasses.Add((charClass.Level, (int)charClass.Class.ClassType));
            }
        }

        // If no base caster class, can't apply prestige bonuses
        if (baseClasses.Count == 0)
        {
            Log.Warn($"Creature {casterCreature.Name} has prestige caster class but no base caster class");
            return;
        }

        // Get the highest level base caster class (dominant caster)
        (int baseLevel, int baseClassConst) baseClassTuple = baseClasses.OrderByDescending(c => c.level).First();

        // Calculate total modifier from all prestige classes
        int totalModifier = 0;
        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            int modifier = _prestigeClassModifiers[prcType](prcLevel);
            totalModifier += modifier;
            Log.Info($"{casterCreature.Name}: Prestige class {prcType} level {prcLevel} adds modifier {modifier}");
        }

        // Calculate final caster level: base plus all prestige modifiers (minimum 1)
        int finalCasterLevel = Math.Max(1, baseClassTuple.baseLevel + totalModifier);

        Log.Info(
            $"{casterCreature.Name}: Setting caster level override - Base {baseClassTuple.baseLevel} + Modifier {totalModifier} = {finalCasterLevel} for class {baseClassTuple.baseClassConst}");

        // Apply the override to the dominant base caster class
        CreaturePlugin.SetCasterLevelOverride(casterCreature, baseClassTuple.baseClassConst, finalCasterLevel);
        _casterLevelOverridesApplied[casterCreature] = true;
    }
}
