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
        { ClassType.ArcaneArcher, prcLevel => Math.Max(0, prcLevel / 2) }
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

        Log.Debug($"[InfiniteCantrip] {player.LoginCreature.Name} cast {obj.Spell.Name} (InnateLevel: {obj.Spell.InnateSpellLevel})");
        player.SendServerMessage($"[DEBUG] Cast {obj.Spell.Name} (Level {obj.Spell.InnateSpellLevel})", ColorConstants.Cyan);

        // Always restore level 0 spells (cantrips)
        if (obj.Spell.InnateSpellLevel == 0)
        {
            Log.Debug($"[InfiniteCantrip] Restoring cantrips for {player.LoginCreature.Name}");
            player.SendServerMessage($"[DEBUG] Restoring cantrips...", ColorConstants.Cyan);

            // Delay restoration to ensure spell slot is consumed first
            _ = NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromMilliseconds(100));
                player.LoginCreature.RestoreSpells(0);
                Log.Debug($"[InfiniteCantrip] Cantrips restored for {player.LoginCreature.Name}");
                player.SendServerMessage($"[DEBUG] Cantrips restored!", ColorConstants.Green);
            });
            return;
        }

        // For level 1 spells, check if effective caster level is >= 20
        if (obj.Spell.InnateSpellLevel == 1)
        {
            int effectiveCasterLevel = GetEffectiveCasterLevel(player.LoginCreature, player);
            Log.Debug($"[InfiniteCantrip] {player.LoginCreature.Name} effective caster level: {effectiveCasterLevel}");
            player.SendServerMessage($"[DEBUG] Effective caster level: {effectiveCasterLevel}", ColorConstants.Cyan);

            if (effectiveCasterLevel >= 20)
            {
                player.SendServerMessage($"[DEBUG] Level >= 20! Restoring level 1 spells...", ColorConstants.Cyan);
                // Delay restoration to ensure spell slot is consumed first
                _ = NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromMilliseconds(100));
                    player.LoginCreature.RestoreSpells(1);
                    Log.Debug($"[InfiniteCantrip] Level 1 spells restored for {player.LoginCreature.Name}");
                    player.SendServerMessage($"[DEBUG] Level 1 spells restored!", ColorConstants.Green);
                });
            }
            else
            {
                player.SendServerMessage($"[DEBUG] Level < 20, not restoring level 1 spells", ColorConstants.Orange);
            }
        }
    }

    private int GetEffectiveCasterLevel(NwCreature creature, NwPlayer player)
    {
        Log.Debug($"[GetEffectiveCasterLevel] Calculating for {creature.Name}");
        player.SendServerMessage($"[DEBUG] === Calculating Effective Caster Level ===", ColorConstants.Yellow);

        // Gather prestige classes
        List<(ClassType classType, int level)> prestigeClasses = [];
        Dictionary<ClassType, int> allBaseClasses = new();

        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            Log.Debug($"[GetEffectiveCasterLevel] Found class: {charClass.Class.ClassType} Level {charClass.Level}");
            player.SendServerMessage($"[DEBUG] Class: {charClass.Class.ClassType} Level {charClass.Level}", ColorConstants.White);

            if (_prestigeClassModifiers.ContainsKey(charClass.Class.ClassType))
            {
                prestigeClasses.Add((charClass.Class.ClassType, charClass.Level));
                Log.Debug($"[GetEffectiveCasterLevel] -> Added as prestige class");
                player.SendServerMessage($"[DEBUG]   -> Prestige class!", ColorConstants.Cyan);
            }

            // Track all potential base classes
            HashSet<ClassType> allValidBaseClasses = PrestigeToBaseCasterMap.Values
                .SelectMany(set => set)
                .ToHashSet();

            if (allValidBaseClasses.Contains(charClass.Class.ClassType))
            {
                allBaseClasses[charClass.Class.ClassType] = charClass.Level;
                Log.Debug($"[GetEffectiveCasterLevel] -> Added as base caster class");
                player.SendServerMessage($"[DEBUG]   -> Base caster class!", ColorConstants.Cyan);
            }
        }

        Log.Debug($"[GetEffectiveCasterLevel] Found {prestigeClasses.Count} prestige classes and {allBaseClasses.Count} base classes");
        player.SendServerMessage($"[DEBUG] Total: {prestigeClasses.Count} prestige, {allBaseClasses.Count} base caster", ColorConstants.Yellow);

        if (prestigeClasses.Count == 0 || allBaseClasses.Count == 0)
        {
            // No prestige classes or no base caster classes - return highest base class level
            int result = allBaseClasses.Count > 0 ? allBaseClasses.Values.Max() : 0;
            Log.Debug($"[GetEffectiveCasterLevel] No prestige/base combo, returning {result}");
            player.SendServerMessage($"[DEBUG] No prestige/base combo, result: {result}", ColorConstants.Orange);
            return result;
        }

        // Dictionary to track effective caster level per base class
        Dictionary<ClassType, (int actualLevel, int modifier)> baseClassBonuses = new();

        // For each prestige class, find its highest valid base class and add modifier
        foreach ((ClassType prcType, int prcLevel) in prestigeClasses)
        {
            Log.Debug($"[GetEffectiveCasterLevel] Processing prestige class: {prcType} Level {prcLevel}");
            player.SendServerMessage($"[DEBUG] Processing {prcType} Level {prcLevel}...", ColorConstants.Cyan);

            if (!PrestigeToBaseCasterMap.TryGetValue(prcType, out HashSet<ClassType>? validBaseClasses))
            {
                Log.Debug($"[GetEffectiveCasterLevel] -> No valid base classes found in map");
                player.SendServerMessage($"[DEBUG]   -> No valid base classes in map", ColorConstants.Red);
                continue;
            }

            Log.Debug($"[GetEffectiveCasterLevel] -> Valid base classes: {string.Join(", ", validBaseClasses)}");
            player.SendServerMessage($"[DEBUG]   Valid bases: {string.Join(", ", validBaseClasses)}", ColorConstants.White);

            // Find the highest level valid base class for this prestige class
            ClassType? selectedBaseForThisPrc = null;
            int highestLevelForThisPrc = 0;

            foreach (var kvp in allBaseClasses)
            {
                if (validBaseClasses.Contains(kvp.Key) && kvp.Value > highestLevelForThisPrc)
                {
                    selectedBaseForThisPrc = kvp.Key;
                    highestLevelForThisPrc = kvp.Value;
                    Log.Debug($"[GetEffectiveCasterLevel] -> Found valid base: {kvp.Key} Level {kvp.Value}");
                    player.SendServerMessage($"[DEBUG]   Selected: {kvp.Key} Level {kvp.Value}", ColorConstants.Green);
                }
            }

            if (selectedBaseForThisPrc == null)
            {
                Log.Debug($"[GetEffectiveCasterLevel] -> No matching base class found");
                player.SendServerMessage($"[DEBUG]   -> No matching base class!", ColorConstants.Red);
                continue;
            }

            // Add this prestige class's bonus to the selected base class
            int modifier = _prestigeClassModifiers[prcType](prcLevel);
            Log.Debug($"[GetEffectiveCasterLevel] -> Modifier for {prcType}: +{modifier}");
            player.SendServerMessage($"[DEBUG]   Modifier: +{modifier}", ColorConstants.Green);

            if (baseClassBonuses.ContainsKey(selectedBaseForThisPrc.Value))
            {
                var existing = baseClassBonuses[selectedBaseForThisPrc.Value];
                baseClassBonuses[selectedBaseForThisPrc.Value] = (existing.actualLevel, existing.modifier + modifier);
                Log.Debug($"[GetEffectiveCasterLevel] -> Updated {selectedBaseForThisPrc}: Level {existing.actualLevel} + {existing.modifier + modifier} modifier");
                player.SendServerMessage($"[DEBUG]   Updated {selectedBaseForThisPrc}: {existing.actualLevel} + {existing.modifier + modifier}", ColorConstants.Cyan);
            }
            else
            {
                baseClassBonuses[selectedBaseForThisPrc.Value] = (highestLevelForThisPrc, modifier);
                Log.Debug($"[GetEffectiveCasterLevel] -> Added {selectedBaseForThisPrc}: Level {highestLevelForThisPrc} + {modifier} modifier");
                player.SendServerMessage($"[DEBUG]   Added {selectedBaseForThisPrc}: {highestLevelForThisPrc} + {modifier}", ColorConstants.Cyan);
            }
        }

        // Find the highest effective caster level across all base classes
        int highestEffectiveCasterLevel = 0;

        foreach (var kvp in baseClassBonuses)
        {
            int effectiveLevel = kvp.Value.actualLevel + kvp.Value.modifier;
            Log.Debug($"[GetEffectiveCasterLevel] {kvp.Key} effective level: {effectiveLevel} ({kvp.Value.actualLevel} + {kvp.Value.modifier})");
            player.SendServerMessage($"[DEBUG] {kvp.Key}: {effectiveLevel} ({kvp.Value.actualLevel} + {kvp.Value.modifier})", ColorConstants.Yellow);
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
                Log.Debug($"[GetEffectiveCasterLevel] Base class without bonus: {kvp.Key} Level {kvp.Value}");
                player.SendServerMessage($"[DEBUG] {kvp.Key} (no bonus): {kvp.Value}", ColorConstants.White);
                highestEffectiveCasterLevel = kvp.Value;
            }
        }

        Log.Debug($"[GetEffectiveCasterLevel] Final result: {highestEffectiveCasterLevel}");
        player.SendServerMessage($"[DEBUG] === Final Effective Level: {highestEffectiveCasterLevel} ===", ColorConstants.Green);
        return highestEffectiveCasterLevel;
    }
}
