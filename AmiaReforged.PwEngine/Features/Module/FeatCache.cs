using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Module;

/// <summary>
/// Caches all valid feats from feat.2da at startup for performance.
/// </summary>
[ServiceBinding(typeof(FeatCache))]
public sealed class FeatCache
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly List<(int id, string name)> _allFeats = new();
    private bool _isInitialized;

    public IReadOnlyList<(int id, string name)> AllFeats => _allFeats;

    public FeatCache()
    {
        // Subscribe to module load to initialize cache at startup
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        Log.Info("Module loaded - initializing feat cache...");
        EnsureInitialized();
    }

    /// <summary>
    /// Ensures the feat cache is loaded. Called automatically on first access.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        Log.Info("Initializing feat cache from feat.2da...");

        try
        {
            // Iterate through potential feat IDs
            // We check up to 1500 to be safe
            for (int i = 0; i < 1500; i++)
            {
                NwFeat? feat = NwFeat.FromFeatId(i);
                if (feat != null)
                {
                    string featName = feat.Name.ToString();

                    // Only include feats with valid names
                    if (!string.IsNullOrWhiteSpace(featName))
                    {
                        _allFeats.Add((i, featName));
                    }
                }
            }

            // Sort alphabetically by name for better UX
            _allFeats.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            _isInitialized = true;
            Log.Info($"Feat cache initialized with {_allFeats.Count} feats.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize feat cache");
            _isInitialized = false;
        }
    }

    /// <summary>
    /// Gets all feats sorted alphabetically by name.
    /// </summary>
    public List<(int id, string name)> GetAllFeats()
    {
        EnsureInitialized();
        return new List<(int id, string name)>(_allFeats);
    }

    /// <summary>
    /// Searches for feats by name (case-insensitive partial match).
    /// </summary>
    public List<(int id, string name)> SearchFeats(string searchTerm)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllFeats();

        string searchLower = searchTerm.ToLowerInvariant();
        return _allFeats
            .Where(f => f.name.ToLowerInvariant().Contains(searchLower))
            .ToList();
    }

    /// <summary>
    /// Gets a feat by ID from the cache (faster than NwFeat.FromFeatId for repeated lookups).
    /// </summary>
    public (int id, string name)? GetFeatById(int featId)
    {
        EnsureInitialized();
        return _allFeats.FirstOrDefault(f => f.id == featId);
    }
}
