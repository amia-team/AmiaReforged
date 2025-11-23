using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(CasterLevelOverrideService))]
public class CasterLevelOverrideService
{
    // Dictionary mapping prestige classes to their caster level modifier formulas
    // Formula takes prestige class level and returns the modifier (minimum 1)
    private readonly Dictionary<ClassType, Func<int, int>> _prestigeClassModifiers = new()
    {
        { ClassType.PaleMaster, prcLevel => Math.Max(1, prcLevel - 5) },
        { ClassType.DragonDisciple, prcLevel => Math.Max(1, prcLevel - 6) }
    };

    // Base caster classes that can receive prestige class bonuses
    private static readonly HashSet<ClassType> BaseCasterClasses = new()
    {
        ClassType.Wizard,
        ClassType.Sorcerer,
        ClassType.Bard,
        ClassType.Assassin,
        ClassType.Druid,
        ClassType.Cleric
    };

    public CasterLevelOverrideService()
    {
        NwModule.Instance.OnClientEnter += FixCasterLevel;
        NwModule.Instance.OnLevelUp += FixCasterLevelOnLevelUp;
        NwModule.Instance.OnLevelDown += FixCasterLevelOnLevelDown;
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
        if (baseClasses.Count == 0) return;

        // Get the highest level base caster class (dominant caster)
        (int baseLevel, int baseClassConst) = baseClasses.OrderByDescending(c => c.level).First();

        // Calculate total modifier from all prestige classes
        int totalModifier = 0;
        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            totalModifier += _prestigeClassModifiers[prcType](prcLevel);
        }

        // Calculate final caster level: base plus all prestige modifiers
        int finalCasterLevel = baseLevel + totalModifier;

        // Apply the override to the dominant base caster class
        CreaturePlugin.SetCasterLevelOverride(casterCreature, baseClassConst, finalCasterLevel);
    }
}
