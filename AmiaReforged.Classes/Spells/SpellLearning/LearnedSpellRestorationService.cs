using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells.SpellLearning;

/// <summary>
/// Service that restores prestige-learned spells for Sorcerers and Bards when they log in.
/// Spells learned through prestige class progression are stored on the ds_pckey item
/// and need to be re-added to the character's spellbook on login.
/// </summary>
[ServiceBinding(typeof(LearnedSpellRestorationService))]
public class LearnedSpellRestorationService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string PrestigeSpellPrefix = "PRESTIGE_SPELL_";

    // Valid base classes that can have prestige spells restored
    private static readonly HashSet<ClassType> SpontaneousCasterClasses = new()
    {
        ClassType.Sorcerer,
        ClassType.Bard
    };

    public LearnedSpellRestorationService()
    {
        NwModule.Instance.OnClientEnter += OnClientEnter;
        Log.Info("Learned Spell Restoration Service initialized.");
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter eventData)
    {
        NwPlayer player = eventData.Player;

        if (player.IsDM)
            return;

        NwCreature? creature = player.LoginCreature;
        if (creature == null)
        {
            Log.Debug($"No login creature for {player.PlayerName}, skipping spell restoration");
            return;
        }

        // Small delay to ensure the character is fully loaded
        NwTask.Run(async () =>
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(500));
            await NwTask.SwitchToMainThread();

            // Re-check creature validity after delay (creature could become invalid in async context)
            if (!creature.IsValid)
            {
                Log.Debug($"Creature no longer valid after delay for {player.PlayerName}");
                return;
            }

            RestorePrestigeSpells(player, creature);
        });
    }

    private void RestorePrestigeSpells(NwPlayer player, NwCreature creature)
    {
        // Get the ds_pckey item for persistent storage
        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(item => item.ResRef == "ds_pckey");
        if (pcKey == null)
        {
            Log.Debug($"No ds_pckey found for {creature.Name}, skipping spell restoration");
            return;
        }

        int totalRestored = 0;

        // Check each spontaneous caster class
        foreach (ClassType classType in SpontaneousCasterClasses)
        {
            CreatureClassInfo? classInfo = creature.GetClassInfo(classType);
            if (classInfo == null)
                continue;

            int classId = classInfo.Class.Id;
            int spellsRestoredForClass = 0;

            // Get all local variables on the pcKey to find prestige spells for this class
            Dictionary<int, HashSet<int>> spellsToRestore = new(); // spellLevel -> set of spellIds

            // Iterate through all local variables to find prestige spell entries
            // Format: PRESTIGE_SPELL_{classType}_{spellLevel}_{spellId}
            string classPrefix = $"{PrestigeSpellPrefix}{classType}_";

            foreach (var localVar in pcKey.LocalVariables)
            {
                if (!localVar.Name.StartsWith(classPrefix))
                    continue;

                // Parse the variable name to extract spell level and spell ID
                string remainder = localVar.Name.Substring(classPrefix.Length);
                string[] parts = remainder.Split('_');

                if (parts.Length != 2)
                {
                    Log.Warn($"Invalid prestige spell variable format: {localVar.Name}");
                    continue;
                }

                if (!int.TryParse(parts[0], out int spellLevel) || !int.TryParse(parts[1], out int spellId))
                {
                    Log.Warn($"Could not parse spell level/id from variable: {localVar.Name}");
                    continue;
                }

                // Only restore if the character's current level is at or above the level when they learned the spell
                int learnedAtLevel = pcKey.GetObjectVariable<LocalVariableInt>(localVar.Name).Value;
                if (learnedAtLevel > 0 && creature.Level >= learnedAtLevel)
                {
                    if (!spellsToRestore.ContainsKey(spellLevel))
                        spellsToRestore[spellLevel] = new HashSet<int>();

                    spellsToRestore[spellLevel].Add(spellId);
                }
            }

            // Now restore the spells for this class
            foreach (var kvp in spellsToRestore)
            {
                int spellLevel = kvp.Key;
                HashSet<int> spellIds = kvp.Value;

                // Get currently known spells at this level
                HashSet<int> currentlyKnown = new();
                IList<NwSpell>? knownSpells = classInfo.KnownSpells.ElementAtOrDefault(spellLevel);
                if (knownSpells != null)
                {
                    foreach (NwSpell spell in knownSpells)
                    {
                        currentlyKnown.Add(spell.Id);
                    }
                }

                // Add spells that aren't already known
                foreach (int spellId in spellIds)
                {
                    if (currentlyKnown.Contains(spellId))
                    {
                        Log.Debug($"Spell {spellId} (Level {spellLevel}) already known for {classType}, skipping");
                        continue;
                    }

                    Log.Info($"Restoring spell {spellId} (Level {spellLevel}) to {creature.Name}'s {classType} spellbook");
                    CreaturePlugin.AddKnownSpell(creature, classId, spellLevel, spellId);
                    spellsRestoredForClass++;
                }
            }

            if (spellsRestoredForClass > 0)
            {
                Log.Info($"Restored {spellsRestoredForClass} prestige spell(s) to {creature.Name}'s {classType} spellbook");
                totalRestored += spellsRestoredForClass;
            }
        }

        if (totalRestored > 0)
        {
            player.SendServerMessage($"Restored {totalRestored} prestige-learned spell(s) to your spellbook.", ColorConstants.Cyan);
            Log.Info($"Total of {totalRestored} prestige spells restored for {creature.Name}");
        }
    }
}


