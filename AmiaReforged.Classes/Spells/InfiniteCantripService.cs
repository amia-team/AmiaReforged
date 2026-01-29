using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(InfiniteCantripService))]
public class InfiniteCantripService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Dictionary mapping prestige classes to their caster level modifier formulas
    private readonly Dictionary<ClassType, Func<int, int>> _prestigeClassModifiers = new()
    {
        { ClassType.PaleMaster, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.DragonDisciple, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.Blackguard, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.DivineChampion, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.ArcaneArcher, prcLevel => Math.Max(0, prcLevel) }
    };

    // Mapping of prestige classes to their valid base caster classes
    private static readonly Dictionary<ClassType, HashSet<ClassType>> PrestigeToBaseCasterMap = new()
    {
        {
            ClassType.PaleMaster,
            new HashSet<ClassType> { ClassType.Wizard, ClassType.Sorcerer }
        },
        {
            ClassType.DragonDisciple,
            new HashSet<ClassType> { ClassType.Sorcerer, ClassType.Bard }
        },
        {
            ClassType.Blackguard,
            new HashSet<ClassType> { ClassType.Cleric, ClassType.Druid, ClassType.Ranger }
        },
        {
            ClassType.DivineChampion,
            new HashSet<ClassType> { ClassType.Cleric, ClassType.Paladin }
        },
        {
            ClassType.ArcaneArcher,
            new HashSet<ClassType> { ClassType.Wizard, ClassType.Sorcerer, ClassType.Bard }
        },
        {
            ClassType.Assassin,
            new HashSet<ClassType> { ClassType.Wizard, ClassType.Sorcerer, ClassType.Bard }
        }
    };

    public InfiniteCantripService(EventService eventService)
    {
        Log.Info(message: "Infinite Cantrip Service initialized.");

        Action<OnSpellCast> onSpellCast = HandleInfiniteCantrip;

        eventService.SubscribeAll<OnSpellCast, OnSpellCast.Factory>(onSpellCast, EventCallbackType.After);
    }

    private void HandleInfiniteCantrip(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (obj.Spell is null) return;
        if (player.LoginCreature is null) return;

        // Get the actual spell level (use MasterSpell if available, as spells can have different levels per class)
        byte spellLevel = obj.Spell.MasterSpell?.InnateSpellLevel ?? obj.Spell.InnateSpellLevel;

        // Always restore level 0 spells (cantrips)
        if (spellLevel == 0)
        {
            // Delay restoration to ensure spell slot is consumed first
            _ = NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromMilliseconds(100));
                player.LoginCreature.RestoreSpells(0);
            });
            return;
        }

        // For level 1 spells, check if effective caster level is >= 20
        if (spellLevel == 1)
        {
            int effectiveCasterLevel = GetEffectiveCasterLevel(player.LoginCreature);

            if (effectiveCasterLevel >= 20)
            {
                // Delay restoration to ensure spell slot is consumed first
                _ = NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromMilliseconds(100));
                    player.LoginCreature.RestoreSpells(1);
                });
            }
        }
    }

    private int GetEffectiveCasterLevel(NwCreature creature)
    {
        // Gather prestige classes
        List<(ClassType classType, int level)> prestigeClasses = [];
        Dictionary<ClassType, int> allBaseClasses = new();

        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            if (_prestigeClassModifiers.ContainsKey(charClass.Class.ClassType))
            {
                prestigeClasses.Add((charClass.Class.ClassType, charClass.Level));
            }

            // Track all potential base classes
            HashSet<ClassType> allValidBaseClasses = PrestigeToBaseCasterMap.Values
                .SelectMany(set => set)
                .ToHashSet();

            if (allValidBaseClasses.Contains(charClass.Class.ClassType))
            {
                allBaseClasses[charClass.Class.ClassType] = charClass.Level;
            }
        }

        if (prestigeClasses.Count == 0 || allBaseClasses.Count == 0)
        {
            // No prestige classes or no base caster classes - return highest base class level
            return allBaseClasses.Count > 0 ? allBaseClasses.Values.Max() : 0;
        }

        // Dictionary to track effective caster level per base class
        Dictionary<ClassType, (int actualLevel, int modifier)> baseClassBonuses = new();

        // For each prestige class, find its highest valid base class and add modifier
        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            if (!PrestigeToBaseCasterMap.TryGetValue(prcType, out HashSet<ClassType>? validBaseClasses))
            {
                continue;
            }

            // Find the highest level valid base class for this prestige class
            ClassType? selectedBaseForThisPrc = null;
            int highestLevelForThisPrc = 0;

            foreach (var kvp in allBaseClasses)
            {
                if (validBaseClasses.Contains(kvp.Key) && kvp.Value > highestLevelForThisPrc)
                {
                    selectedBaseForThisPrc = kvp.Key;
                    highestLevelForThisPrc = kvp.Value;
                }
            }

            if (selectedBaseForThisPrc == null)
            {
                continue;
            }

            // Add this prestige class's bonus to the selected base class
            int modifier = _prestigeClassModifiers[prcType](prcLevel);

            if (baseClassBonuses.ContainsKey(selectedBaseForThisPrc.Value))
            {
                var existing = baseClassBonuses[selectedBaseForThisPrc.Value];
                baseClassBonuses[selectedBaseForThisPrc.Value] = (existing.actualLevel, existing.modifier + modifier);
            }
            else
            {
                baseClassBonuses[selectedBaseForThisPrc.Value] = (highestLevelForThisPrc, modifier);
            }
        }

        // Find the highest effective caster level across all base classes
        int highestEffectiveCasterLevel = 0;

        foreach (var kvp in baseClassBonuses)
        {
            int effectiveLevel = kvp.Value.actualLevel + kvp.Value.modifier;
            if (effectiveLevel > highestEffectiveCasterLevel)
            {
                highestEffectiveCasterLevel = effectiveLevel;
            }
        }

        // Also consider base classes without prestige bonuses
        foreach (var kvp in allBaseClasses)
        {
            if (!baseClassBonuses.ContainsKey(kvp.Key) && kvp.Value > highestEffectiveCasterLevel)
            {
                highestEffectiveCasterLevel = kvp.Value;
            }
        }

        return highestEffectiveCasterLevel;
    }
}
