using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells.SpellLearning;

/// <summary>
/// Service that handles spell learning for Sorcerers and Bards when taking prestige caster levels.
/// Triggers a NUI after level up to allow players to select spells based on their effective caster level.
/// </summary>
[ServiceBinding(typeof(SpellLearningService))]
public class SpellLearningService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;

    // Prestige classes that trigger spell learning for Sorcerer/Bard
    private static readonly HashSet<ClassType> PrestigeCasterClasses = new()
    {
        ClassType.DragonDisciple,
        ClassType.PaleMaster,
        ClassType.ArcaneArcher
    };

    // Valid base classes for spell learning (spontaneous casters only)
    private static readonly HashSet<ClassType> SpontaneousCasterClasses = new()
    {
        ClassType.Sorcerer,
        ClassType.Bard
    };

    // Mapping of prestige classes to their caster level progression
    private static readonly Dictionary<ClassType, Func<int, int>> PrestigeCasterLevelModifiers = new()
    {
        { ClassType.DragonDisciple, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.PaleMaster, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.ArcaneArcher, prcLevel => Math.Max(0, prcLevel / 2) }
    };

    // Valid base classes for each prestige class
    private static readonly Dictionary<ClassType, HashSet<ClassType>> PrestigeToBaseClassMap = new()
    {
        {
            ClassType.DragonDisciple,
            new HashSet<ClassType> { ClassType.Sorcerer, ClassType.Bard }
        },
        {
            ClassType.PaleMaster,
            new HashSet<ClassType> { ClassType.Sorcerer, ClassType.Bard } // Wizard is memorization, Assassin needs special handling
        },
        {
            ClassType.ArcaneArcher,
            new HashSet<ClassType> { ClassType.Sorcerer, ClassType.Bard }
        }
    };

    public SpellLearningService(EventService eventService)
    {
        // Subscribe to level up events
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUp, EventCallbackType.After);

        // Subscribe to level down events to clean up prestige spells
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDown, EventCallbackType.After);

        Log.Info("Spell Learning Service initialized.");
    }

    private void OnLevelUp(OnLevelUp obj)
    {
        NwCreature creature = obj.Creature;

        if (!creature.IsPlayerControlled(out NwPlayer? player))
        {
            Log.Debug($"Creature {creature.Name} is not player controlled, skipping");
            return;
        }

        // Get the level info for the level they just gained
        CreatureLevelInfo? levelInfo = creature.LevelInfo.LastOrDefault();
        if (levelInfo == null)
        {
            Log.Warn($"Could not get level info for {creature.Name}");
            return;
        }

        // Check if they just leveled in a prestige caster class
        ClassType leveledClass = levelInfo.ClassInfo.Class.ClassType;

        Log.Debug($"{creature.Name} leveled up in {leveledClass}");

        if (!PrestigeCasterClasses.Contains(leveledClass))
        {
            Log.Debug($"{leveledClass} is not a prestige caster class, skipping");
            return;
        }

        Log.Info($"{creature.Name} leveled up in {leveledClass}, checking for spell learning eligibility...");

        // Determine which base class to boost
        var eligibleBaseClass = GetEligibleBaseClass(creature, leveledClass);

        if (eligibleBaseClass == null)
        {
            Log.Info($"No eligible spontaneous caster base class found for {creature.Name}");
            return;
        }

        ClassType baseClass = eligibleBaseClass.Value;
        int baseLevel = creature.GetClassInfo(baseClass)?.Level ?? 0;
        int effectiveCasterLevel = CalculateEffectiveCasterLevel(baseClass, baseLevel, creature);

        Log.Info($"{creature.Name} is eligible for spell learning: Base={baseClass} L{baseLevel}, Effective L{effectiveCasterLevel}");

        // Get current spells known for this class
        Dictionary<int, int> currentSpellsKnown = GetCurrentSpellsKnown(creature, baseClass);

        int totalKnown = currentSpellsKnown.Values.Sum();
        Log.Info($"Current spells known (total: {totalKnown}): {string.Join(", ", currentSpellsKnown.Where(kvp => kvp.Value > 0).Select(kvp => $"L{kvp.Key}={kvp.Value}"))}");

        // Check if there are any new spells to learn by comparing current to target
        Dictionary<int, int> spellsNeeded = SpellProgressionData.GetSpellsNeededToReachLevel(baseClass, currentSpellsKnown, effectiveCasterLevel);

        // Log what they should have at this effective level
        Dictionary<int, int> shouldHave = new();
        for (int i = 0; i <= 9; i++)
        {
            int count = baseClass == ClassType.Sorcerer
                ? SpellProgressionData.GetSorcererSpellsKnown(effectiveCasterLevel, i)
                : SpellProgressionData.GetBardSpellsKnown(effectiveCasterLevel, i);
            if (count > 0) shouldHave[i] = count;
        }
        int totalShould = shouldHave.Values.Sum();
        Log.Info($"Should have at effective L{effectiveCasterLevel} (total: {totalShould}): {string.Join(", ", shouldHave.Select(kvp => $"L{kvp.Key}={kvp.Value}"))}");


        if (spellsNeeded.Count == 0)
        {
            Log.Info($"{creature.Name} has no new spells to learn at effective caster level {effectiveCasterLevel}");
            return;
        }

        Log.Info($"{creature.Name} needs to learn spells: {string.Join(", ", spellsNeeded.Select(kvp => $"L{kvp.Key}={kvp.Value}"))}");

        // Trigger the spell learning NUI
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(500)); // Small delay to let level up finish
            ShowSpellLearningNui(player, baseClass, effectiveCasterLevel, spellsNeeded);
        });
    }

    private void OnLevelDown(OnLevelDown obj)
    {
        NwCreature creature = obj.Creature;

        if (!creature.IsPlayerControlled(out NwPlayer? player))
        {
            Log.Debug($"Creature {creature.Name} is not player controlled, skipping delevel spell cleanup");
            return;
        }

        int newLevel = creature.Level;
        Log.Info($"{creature.Name} deleveled to {newLevel}, checking for prestige spells to remove...");

        // Get the ds_pckey item for persistent storage
        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey == null)
        {
            Log.Warn($"Could not find ds_pckey item for {creature.Name}, cannot clean up prestige spells");
            return;
        }

        // Check all spontaneous caster classes
        foreach (ClassType classType in SpontaneousCasterClasses)
        {
            CreatureClassInfo? classInfo = creature.GetClassInfo(classType);
            if (classInfo == null)
                continue;

            int classId = classInfo.Class.Id;
            int spellsRemoved = 0;

            // Check all spell levels (0-9) and all possible spell IDs
            for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
            {
                // Get all known spells at this level
                IList<NwSpell>? knownSpells = classInfo.KnownSpells.ElementAtOrDefault(spellLevel);
                if (knownSpells == null)
                    continue;

                // Check each spell to see if it was learned at a higher level
                List<int> spellsToRemove = new();

                foreach (NwSpell spell in knownSpells)
                {
                    string persistentKey = $"PRESTIGE_SPELL_{classType}_{spellLevel}_{spell.Id}";
                    int learnedAtLevel = pcKey.GetObjectVariable<LocalVariableInt>(persistentKey).Value;

                    if (learnedAtLevel > 0 && newLevel < learnedAtLevel)
                    {
                        spellsToRemove.Add(spell.Id);
                        Log.Info($"Marking spell {spell.Id} ({spell.Name}) for removal (learned at L{learnedAtLevel}, now L{newLevel})");
                    }
                }

                // Remove the spells
                foreach (int spellId in spellsToRemove)
                {
                    CreaturePlugin.RemoveKnownSpell(creature, classId, spellLevel, spellId);

                    // Clean up the persistent variable from ds_pckey
                    string persistentKey = $"PRESTIGE_SPELL_{classType}_{spellLevel}_{spellId}";
                    pcKey.GetObjectVariable<LocalVariableInt>(persistentKey).Delete();

                    spellsRemoved++;
                }
            }

            if (spellsRemoved > 0)
            {
                player.SendServerMessage($"Removed {spellsRemoved} prestige spell(s) from your {classType} spellbook due to level change.", ColorConstants.Orange);
                Log.Info($"Removed {spellsRemoved} prestige spells from {creature.Name}'s {classType} spellbook");
            }
        }
    }

    private Dictionary<int, int> GetCurrentSpellsKnown(NwCreature creature, ClassType classType)
    {
        Dictionary<int, int> spellsKnown = new();

        CreatureClassInfo? classInfo = creature.GetClassInfo(classType);
        if (classInfo == null)
            return spellsKnown;

        // Count known spells at each spell level (0-9)
        for (int spellLevel = 0; spellLevel <= 9; spellLevel++)
        {
            IList<NwSpell>? knownSpells = classInfo.KnownSpells.ElementAtOrDefault(spellLevel);
            spellsKnown[spellLevel] = knownSpells?.Count ?? 0;
        }

        return spellsKnown;
    }

    private ClassType? GetEligibleBaseClass(NwCreature creature, ClassType prestigeClass)
    {
        if (!PrestigeToBaseClassMap.TryGetValue(prestigeClass, out HashSet<ClassType>? validBaseClasses))
        {
            Log.Warn($"No base class mapping found for prestige class {prestigeClass}");
            return null;
        }

        Log.Debug($"Valid base classes for {prestigeClass}: {string.Join(", ", validBaseClasses)}");

        // Find the highest level valid spontaneous caster base class
        ClassType? selectedBase = null;
        int highestLevel = 0;

        foreach (CreatureClassInfo classInfo in creature.Classes)
        {
            Log.Debug($"Checking class {classInfo.Class.ClassType} (Level {classInfo.Level})");

            if (validBaseClasses.Contains(classInfo.Class.ClassType) &&
                SpontaneousCasterClasses.Contains(classInfo.Class.ClassType))
            {
                Log.Debug($"  -> {classInfo.Class.ClassType} is valid and spontaneous");
                if (classInfo.Level > highestLevel)
                {
                    highestLevel = classInfo.Level;
                    selectedBase = classInfo.Class.ClassType;
                    Log.Debug($"  -> Selected as highest level ({classInfo.Level})");
                }
            }
        }

        if (selectedBase.HasValue)
        {
            Log.Info($"Selected base class: {selectedBase.Value} (Level {highestLevel})");
        }
        else
        {
            Log.Warn($"No eligible base class found for {creature.Name}");
        }

        return selectedBase;
    }

    private int CalculateEffectiveCasterLevel(ClassType baseClass, int baseLevel, NwCreature creature)
    {
        int totalModifier = 0;

        foreach (CreatureClassInfo classInfo in creature.Classes)
        {
            if (PrestigeCasterLevelModifiers.TryGetValue(classInfo.Class.ClassType, out Func<int, int>? modifierFunc))
            {
                // Check if this prestige class can boost the base class
                if (PrestigeToBaseClassMap.TryGetValue(classInfo.Class.ClassType, out HashSet<ClassType>? validBases) &&
                    validBases.Contains(baseClass))
                {
                    totalModifier += modifierFunc(classInfo.Level);
                }
            }
        }

        return baseLevel + totalModifier;
    }

    private void ShowSpellLearningNui(NwPlayer player, ClassType baseClass, int effectiveCasterLevel, Dictionary<int, int> spellsNeeded)
    {
        Log.Info($"Showing spell learning NUI for {player.PlayerName}: {baseClass} at Effective L{effectiveCasterLevel}");

        try
        {
            SpellLearningView view = new(player, baseClass, effectiveCasterLevel, spellsNeeded);

            // Use WindowDirector to properly register the window for event routing
            WindowDirector.Value.OpenWindow(view.Presenter);

            Log.Info($"Successfully created spell learning NUI for {player.PlayerName}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to create spell learning NUI for {player.PlayerName}");
            player.SendServerMessage($"Error opening spell learning window. Please report this to a DM.", ColorConstants.Red);
        }
    }
}

