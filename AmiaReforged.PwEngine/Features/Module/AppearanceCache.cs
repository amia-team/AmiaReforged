using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Module;

/// <summary>
/// Caches valid appearances from appearance.2da at startup for performance.
/// Only includes appearances with allowed racial types for player use.
/// </summary>
[ServiceBinding(typeof(AppearanceCache))]
public sealed class AppearanceCache
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly List<(int id, string label)> _allAppearances = new();
    private bool _isInitialized;

    // Allowed racial types for appearance changes (matches ThousandFacesModel)
    private static readonly HashSet<int> AllowedRacialTypes = [0, 1, 2, 3, 4, 5, 6, 8, 12, 13, 14, 15, 25];

    public IReadOnlyList<(int id, string label)> AllAppearances => _allAppearances;

    public AppearanceCache()
    {
        // Subscribe to module load to initialize cache at startup
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        Log.Info("Module loaded - initializing appearance cache...");
        EnsureInitialized();
    }

    /// <summary>
    /// Ensures the appearance cache is loaded. Called automatically on first access.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        Log.Info("Initializing appearance cache from appearance.2da...");

        try
        {
            // Get the row count from appearance.2da
            // We check up to 3000 to be safe (appearance.2da can be large)
            for (int i = 0; i < 3000; i++)
            {
                // Get the LABEL column
                string label = NWScript.Get2DAString("appearance", "LABEL", i);

                // Skip if no label (invalid row)
                if (string.IsNullOrWhiteSpace(label) || label == "****")
                    continue;

                // Get the RACIALTYPE column
                string racialTypeStr = NWScript.Get2DAString("appearance", "RACIALTYPE", i);

                // Skip if no racial type or if it's not a valid number
                if (string.IsNullOrWhiteSpace(racialTypeStr) || racialTypeStr == "****")
                    continue;

                if (!int.TryParse(racialTypeStr, out int racialType))
                    continue;

                // Only include if the racial type is in our allowed list
                if (AllowedRacialTypes.Contains(racialType))
                {
                    _allAppearances.Add((i, label));
                }
            }

            // Sort alphabetically by label for better UX
            _allAppearances.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));

            _isInitialized = true;
            Log.Info($"Appearance cache initialized with {_allAppearances.Count} valid appearances.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize appearance cache");
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Gets all valid appearances sorted alphabetically by label.
    /// </summary>
    public List<(int id, string label)> GetAllAppearances()
    {
        EnsureInitialized();
        return new List<(int id, string label)>(_allAppearances);
    }

    /// <summary>
    /// Searches for appearances by label (case-insensitive partial match).
    /// </summary>
    public List<(int id, string label)> SearchAppearances(string searchTerm)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllAppearances();

        string searchLower = searchTerm.ToLowerInvariant();
        return _allAppearances
            .Where(a => a.label.ToLowerInvariant().Contains(searchLower))
            .ToList();
    }

    /// <summary>
    /// Gets an appearance by ID from the cache.
    /// </summary>
    public (int id, string label)? GetAppearance(int id)
    {
        EnsureInitialized();
        return _allAppearances.FirstOrDefault(a => a.id == id);
    }
}
