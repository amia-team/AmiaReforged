using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Caches all divine spells (by class and level) and domain spells (from domains.2da) at startup.
/// Used by DivineCasterSpellAccessService to grant spells to prestige-boosted divine casters.
/// </summary>
[ServiceBinding(typeof(DivineSpellCache))]
public sealed class DivineSpellCache
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Divine caster classes we cache spells for
    private static readonly ClassType[] DivineCasterClasses =
    {
        ClassType.Cleric,
        ClassType.Druid,
        ClassType.Paladin,
        ClassType.Ranger
    };

    // Class spells: ClassType -> SpellLevel -> List of spell IDs
    private readonly Dictionary<ClassType, Dictionary<int, List<int>>> _classSpells = new();

    // Domain spells: DomainId -> SpellLevel -> SpellId (one spell per level per domain)
    private readonly Dictionary<int, Dictionary<int, int>> _domainSpells = new();

    // Domain names for logging
    private readonly Dictionary<int, string> _domainNames = new();

    private bool _isInitialized;

    public DivineSpellCache()
    {
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        Log.Info("Module loaded - initializing divine spell cache...");
        EnsureInitialized();
    }

    /// <summary>
    /// Ensures the cache is loaded. Called automatically on first access.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        try
        {
            CacheClassSpells();
            CacheDomainSpells();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize divine spell cache");
            _isInitialized = false;
        }
    }

    private void CacheClassSpells()
    {
        Log.Info("Caching divine class spells from NwRuleset.Spells...");

        // Initialize dictionaries for each class
        foreach (ClassType classType in DivineCasterClasses)
        {
            _classSpells[classType] = new Dictionary<int, List<int>>();
            for (int level = 0; level <= 9; level++)
            {
                _classSpells[classType][level] = new List<int>();
            }
        }

        // Get NwClass objects for each divine class
        Dictionary<ClassType, NwClass?> nwClasses = new();
        foreach (ClassType classType in DivineCasterClasses)
        {
            nwClasses[classType] = NwClass.FromClassType(classType);
        }

        // Iterate through all spells and categorize them
        int totalSpellsCached = 0;
        foreach (NwSpell spell in NwRuleset.Spells)
        {
            foreach (ClassType classType in DivineCasterClasses)
            {
                NwClass? nwClass = nwClasses[classType];
                if (nwClass == null) continue;

                try
                {
                    // GetSpellLevelForClass returns 255 if the spell is not available for this class
#pragma warning disable CS0618 // Type or member is obsolete
                    byte spellLevel = spell.GetSpellLevelForClass(nwClass);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (spellLevel != 255 && spellLevel <= 9)
                    {
                        _classSpells[classType][spellLevel].Add(spell.Id);
                        totalSpellsCached++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, $"Error checking spell {spell.Id} ({spell.Name}) for class {classType}");
                }
            }
        }

        // Log summary
        Log.Info($"Divine class spell cache complete. Total spell entries: {totalSpellsCached}");
        foreach (ClassType classType in DivineCasterClasses)
        {
            int classTotal = _classSpells[classType].Values.Sum(list => list.Count);
            string levelBreakdown = string.Join(", ",
                _classSpells[classType]
                    .Where(kvp => kvp.Value.Count > 0)
                    .Select(kvp => $"L{kvp.Key}={kvp.Value.Count}"));
            Log.Info($"  {classType}: {classTotal} spells ({levelBreakdown})");
        }
    }

    private void CacheDomainSpells()
    {
        Log.Info("Caching domain spells from domains.2da...");

        var domainsTable = NwGameTables.GetTable("domains");
        if (domainsTable == null)
        {
            Log.Error("Could not load domains.2da table!");
            return;
        }

        int domainsLoaded = 0;
        int domainSpellsLoaded = 0;

        // Iterate through all rows in domains.2da
        // Row number = Domain ID
        for (int domainId = 0; domainId < 100; domainId++) // Check up to 100 domains to be safe
        {
            string? label = domainsTable.GetString(domainId, "Label");

            // Skip empty rows
            if (string.IsNullOrEmpty(label) || label == "****")
                continue;

            _domainNames[domainId] = label;
            _domainSpells[domainId] = new Dictionary<int, int>();

            // Read spell IDs for each spell level (Level_1 through Level_9)
            for (int spellLevel = 1; spellLevel <= 9; spellLevel++)
            {
                string columnName = $"Level_{spellLevel}";
                string? spellIdStr = domainsTable.GetString(domainId, columnName);

                // Skip empty/invalid entries (****)
                if (string.IsNullOrEmpty(spellIdStr) || spellIdStr == "****")
                    continue;

                // Parse the spell ID - note that 0 is a valid spell ID
                if (int.TryParse(spellIdStr, out int spellId))
                {
                    _domainSpells[domainId][spellLevel] = spellId;
                    domainSpellsLoaded++;
                }
            }

            domainsLoaded++;
        }

        Log.Info($"Domain spell cache complete. Loaded {domainsLoaded} domains with {domainSpellsLoaded} total domain spells.");

        // Log details for debugging
        foreach (var kvp in _domainSpells.Where(d => d.Value.Count > 0))
        {
            string domainName = _domainNames.TryGetValue(kvp.Key, out string? name) ? name : $"Domain_{kvp.Key}";
            string spellLevels = string.Join(", ", kvp.Value.Select(s => $"L{s.Key}={s.Value}"));
            Log.Debug($"  {domainName} (ID {kvp.Key}): {spellLevels}");
        }
    }

    /// <summary>
    /// Gets all spell IDs for a specific class and spell level.
    /// </summary>
    public IReadOnlyList<int> GetSpellsForClass(ClassType classType, int spellLevel)
    {
        EnsureInitialized();

        if (_classSpells.TryGetValue(classType, out var levelDict) &&
            levelDict.TryGetValue(spellLevel, out var spells))
        {
            return spells;
        }

        return Array.Empty<int>();
    }

    /// <summary>
    /// Gets all spell IDs for a class up to and including a maximum spell level.
    /// </summary>
    public IEnumerable<(int spellLevel, int spellId)> GetSpellsForClassUpToLevel(ClassType classType, int maxSpellLevel)
    {
        EnsureInitialized();

        if (!_classSpells.TryGetValue(classType, out var levelDict))
            yield break;

        for (int level = 0; level <= maxSpellLevel; level++)
        {
            if (levelDict.TryGetValue(level, out var spells))
            {
                foreach (int spellId in spells)
                {
                    yield return (level, spellId);
                }
            }
        }
    }

    /// <summary>
    /// Gets the domain spell ID for a specific domain and spell level.
    /// Returns -1 if no spell exists for that domain/level combination.
    /// </summary>
    public int GetDomainSpell(int domainId, int spellLevel)
    {
        EnsureInitialized();

        if (_domainSpells.TryGetValue(domainId, out var levelDict) &&
            levelDict.TryGetValue(spellLevel, out int spellId))
        {
            return spellId;
        }

        return -1;
    }

    /// <summary>
    /// Gets all domain spells for a domain up to and including a maximum spell level.
    /// </summary>
    public IEnumerable<(int spellLevel, int spellId)> GetDomainSpellsUpToLevel(int domainId, int maxSpellLevel)
    {
        EnsureInitialized();

        if (!_domainSpells.TryGetValue(domainId, out var levelDict))
            yield break;

        for (int level = 1; level <= maxSpellLevel; level++)
        {
            if (levelDict.TryGetValue(level, out int spellId))
            {
                yield return (level, spellId);
            }
        }
    }

    /// <summary>
    /// Gets the name of a domain by ID.
    /// </summary>
    public string GetDomainName(int domainId)
    {
        EnsureInitialized();
        return _domainNames.TryGetValue(domainId, out string? name) ? name : $"Unknown Domain ({domainId})";
    }

    /// <summary>
    /// Checks if the cache has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;
}

