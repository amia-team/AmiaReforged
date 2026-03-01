using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;

/// <summary>
/// Manages caching of the area graph to memory and disk.
/// The graph is computed on demand and cached until explicitly refreshed.
/// </summary>
public class AreaGraphCacheService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AreaGraphBuilder _builder;
    private readonly string _cacheFilePath;
    private AreaGraphData? _cached;

    public AreaGraphCacheService(AreaGraphBuilder builder, string? cacheDirectory = null)
    {
        _builder = builder;

        string dir = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AmiaReforged", "WorldEngine");

        Directory.CreateDirectory(dir);
        _cacheFilePath = Path.Combine(dir, "areagraph.json");
    }

    /// <summary>
    /// Returns the current graph, building and caching it if needed.
    /// </summary>
    /// <param name="forceRefresh">When true, rebuilds the graph even if a cache exists.</param>
    public async Task<AreaGraphData> GetOrBuildAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cached != null)
        {
            return _cached;
        }

        if (!forceRefresh && _cached == null)
        {
            // Try loading from disk
            var loaded = LoadFromDisk();
            if (loaded != null)
            {
                _cached = loaded;
                return _cached;
            }
        }

        // Build fresh — this will switch to the main thread internally
        Log.Info("Building fresh area graph (forceRefresh={ForceRefresh})...", forceRefresh);
        _cached = await _builder.BuildAsync();
        SaveToDisk(_cached);
        return _cached;
    }

    /// <summary>
    /// Forces a full rebuild, updates cache, and returns the new graph.
    /// </summary>
    public async Task<AreaGraphData> RefreshAsync()
    {
        return await GetOrBuildAsync(forceRefresh: true);
    }

    private void SaveToDisk(AreaGraphData data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, SerializerOptions);
            File.WriteAllText(_cacheFilePath, json);
            Log.Info("Area graph saved to {Path}", _cacheFilePath);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to save area graph to disk at {Path}", _cacheFilePath);
        }
    }

    private AreaGraphData? LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            string json = File.ReadAllText(_cacheFilePath);
            var data = JsonSerializer.Deserialize<AreaGraphData>(json, SerializerOptions);
            if (data != null)
            {
                Log.Info("Loaded area graph from disk ({Nodes} nodes, {Edges} edges)",
                    data.Nodes.Count, data.Edges.Count);
            }

            return data;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to load area graph from disk at {Path}", _cacheFilePath);
            return null;
        }
    }
}
