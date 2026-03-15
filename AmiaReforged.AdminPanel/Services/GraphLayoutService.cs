using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmiaReforged.AdminPanel.Models;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Miscellaneous;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Computes graph layouts server-side using MSAGL and custom algorithms.
/// Results are cached in-memory and on disk to avoid recomputation.
/// </summary>
public class GraphLayoutService
{
    private readonly ILogger<GraphLayoutService> _logger;

    // In-memory cache: cacheKey → layout result
    private readonly ConcurrentDictionary<string, GraphLayoutResult> _memoryCache = new();

    // Disk cache directory
    private readonly string _cacheDir;

    // Prevent concurrent computation of the same layout
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _computeLocks = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Node dimensions for MSAGL layout (matching Cytoscape node sizes)
    private const double NodeWidth = 24;
    private const double NodeHeight = 24;
    private const double RegionPadding = 20;

    public GraphLayoutService(ILogger<GraphLayoutService> logger)
    {
        _logger = logger;
        _cacheDir = Path.Combine(AppContext.BaseDirectory, "layout-cache");
        Directory.CreateDirectory(_cacheDir);
        _logger.LogInformation("GraphLayoutService initialized. Cache dir: {CacheDir}", _cacheDir);
    }

    /// <summary>
    /// Compute a graph layout, using cache if available.
    /// </summary>
    public async Task<GraphLayoutResult> ComputeLayoutAsync(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        string algorithm,
        Dictionary<string, object>? config = null,
        IProgress<(int Percent, string Phase)>? progress = null,
        CancellationToken ct = default)
    {
        string cacheKey = BuildCacheKey(graphData, regions, algorithm, config);

        // Check memory cache
        if (_memoryCache.TryGetValue(cacheKey, out GraphLayoutResult? cached))
        {
            _logger.LogDebug("Layout cache hit (memory): {Algorithm}", algorithm);
            progress?.Report((100, "Complete (cached)"));
            return cached;
        }

        // Check disk cache
        GraphLayoutResult? diskCached = await LoadFromDiskCacheAsync(cacheKey, ct);
        if (diskCached != null)
        {
            _logger.LogDebug("Layout cache hit (disk): {Algorithm}", algorithm);
            _memoryCache[cacheKey] = diskCached;
            progress?.Report((100, "Complete (cached)"));
            return diskCached;
        }

        // Compute layout (with lock to prevent duplicate computation)
        SemaphoreSlim computeLock = _computeLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await computeLock.WaitAsync(ct);
        try
        {
            // Double-check cache after acquiring lock
            if (_memoryCache.TryGetValue(cacheKey, out cached))
            {
                progress?.Report((100, "Complete (cached)"));
                return cached;
            }

            progress?.Report((5, "Computing layout..."));

            GraphLayoutResult result = await Task.Run(() =>
                ComputeLayoutInternal(graphData, regions, algorithm, config, progress, ct), ct);

            // Cache the result
            _memoryCache[cacheKey] = result;
            _ = SaveToDiskCacheAsync(cacheKey, result); // fire-and-forget

            progress?.Report((100, "Complete"));
            return result;
        }
        finally
        {
            computeLock.Release();
        }
    }

    /// <summary>
    /// Invalidate all cached layouts (call when graph topology changes).
    /// </summary>
    public void InvalidateCache()
    {
        _memoryCache.Clear();
        try
        {
            if (Directory.Exists(_cacheDir))
            {
                foreach (string file in Directory.GetFiles(_cacheDir, "*.json"))
                {
                    File.Delete(file);
                }
            }
            _logger.LogInformation("Layout cache invalidated");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear disk layout cache");
        }
    }

    private GraphLayoutResult ComputeLayoutInternal(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        string algorithm,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress,
        CancellationToken ct)
    {
        Dictionary<string, GraphLayoutPosition> positions = algorithm switch
        {
            "cose" or "fcose" => ComputeMdsLayout(graphData, regions, config, progress, ct),
            "dagre" => ComputeLayeredLayout(graphData, regions, config, progress, ct),
            "compact-grid" => ComputeCompactGridLayout(graphData, regions, config, progress),
            "grid" => ComputeGridLayout(graphData, regions, config, progress),
            "circle" => ComputeCircleLayout(graphData, regions, config, progress),
            "concentric" => ComputeConcentricLayout(graphData, regions, config, progress),
            "breadthfirst" => ComputeLayeredLayout(graphData, regions, config, progress, ct),
            _ => ComputeMdsLayout(graphData, regions, config, progress, ct)
        };

        return new GraphLayoutResult
        {
            Positions = positions,
            Algorithm = algorithm,
            ComputedAtUtc = DateTime.UtcNow
        };
    }

    // ==================== MSAGL MDS (Force-Directed) Layout ====================

    private Dictionary<string, GraphLayoutPosition> ComputeMdsLayout(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress,
        CancellationToken ct)
    {
        progress?.Report((10, "Building graph model..."));

        (GeometryGraph graph, Dictionary<string, Node> nodeMap) =
            BuildMsaglGraph(graphData, regions);

        progress?.Report((30, "Running force-directed layout..."));
        ct.ThrowIfCancellationRequested();

        var settings = new MdsLayoutSettings
        {
            ScaleX = GetConfigDouble(config, "idealEdgeLength", 50),
            ScaleY = GetConfigDouble(config, "idealEdgeLength", 50),
        };

        try
        {
            LayoutHelpers.CalculateLayout(graph, settings, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MDS layout failed, falling back to manual spread");
            return ComputeCompactGridLayout(graphData, regions, config, progress);
        }

        progress?.Report((85, "Extracting positions..."));

        return ExtractPositions(graph, nodeMap);
    }

    // ==================== MSAGL Layered (Sugiyama/Dagre-like) Layout ====================

    private Dictionary<string, GraphLayoutPosition> ComputeLayeredLayout(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress,
        CancellationToken ct)
    {
        progress?.Report((10, "Building graph model..."));

        (GeometryGraph graph, Dictionary<string, Node> nodeMap) =
            BuildMsaglGraph(graphData, regions);

        progress?.Report((30, "Running hierarchical layout..."));
        ct.ThrowIfCancellationRequested();

        string rankDir = GetConfigString(config, "rankDir", "LR");
        var settings = new SugiyamaLayoutSettings
        {
            NodeSeparation = GetConfigDouble(config, "nodeSep", 18),
            LayerSeparation = GetConfigDouble(config, "rankSep", 35),
        };

        // Set direction based on rankDir
        switch (rankDir.ToUpperInvariant())
        {
            case "TB":
                settings.Transformation = PlaneTransformation.Rotation(0);
                break;
            case "BT":
                settings.Transformation = PlaneTransformation.Rotation(Math.PI);
                break;
            case "RL":
                settings.Transformation = PlaneTransformation.Rotation(-Math.PI / 2);
                break;
            case "LR":
            default:
                settings.Transformation = PlaneTransformation.Rotation(Math.PI / 2);
                break;
        }

        try
        {
            LayoutHelpers.CalculateLayout(graph, settings, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sugiyama layout failed, falling back to MDS");
            return ComputeMdsLayout(graphData, regions, config, progress, ct);
        }

        progress?.Report((85, "Extracting positions..."));

        return ExtractPositions(graph, nodeMap);
    }

    // ==================== Custom Compact Grid Layout ====================

    private Dictionary<string, GraphLayoutPosition> ComputeCompactGridLayout(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress)
    {
        progress?.Report((10, "Computing compact grid..."));

        int nodeSpacing = GetConfigInt(config, "nodeSpacing", 32);
        int regionGap = GetConfigInt(config, "regionGap", 24);
        int orphanGroupGap = GetConfigInt(config, "orphanGroupGap", 12);

        // Build region -> area mapping
        Dictionary<string, List<string>> regionAreas = new();
        Dictionary<string, string> areaToRegion = new();

        foreach (RegionInfo region in regions)
        {
            regionAreas[region.Tag] = new List<string>(region.AreaResRefs);
            foreach (string resRef in region.AreaResRefs)
            {
                areaToRegion[resRef.ToLowerInvariant()] = region.Tag;
            }
        }

        // Also check node.Region field
        List<AreaNodeDto> allNodes = graphData.Nodes.Concat(graphData.DisconnectedAreas).ToList();
        foreach (AreaNodeDto node in allNodes)
        {
            string lower = node.ResRef.ToLowerInvariant();
            if (!areaToRegion.ContainsKey(lower) && !string.IsNullOrEmpty(node.Region))
            {
                areaToRegion[lower] = node.Region;
                if (!regionAreas.ContainsKey(node.Region))
                    regionAreas[node.Region] = new List<string>();
                regionAreas[node.Region].Add(node.ResRef);
            }
        }

        // Build region blocks
        List<(string? RegionTag, List<string> NodeIds, bool IsOrphan)> blocks = new();

        foreach (var (tag, areaRefs) in regionAreas.OrderByDescending(kv => kv.Value.Count))
        {
            if (areaRefs.Count > 0)
                blocks.Add((tag, areaRefs, false));
        }

        // Orphan nodes (not assigned to any region)
        HashSet<string> assignedNodes = new(areaToRegion.Keys, StringComparer.OrdinalIgnoreCase);
        List<string> orphans = allNodes
            .Where(n => !assignedNodes.Contains(n.ResRef.ToLowerInvariant()))
            .Select(n => n.ResRef)
            .ToList();

        // Group orphans by name prefix
        if (orphans.Count > 0)
        {
            Dictionary<string, List<string>> prefixGroups = new();
            Dictionary<string, string> nodeNames = allNodes.ToDictionary(
                n => n.ResRef, n => n.Name, StringComparer.OrdinalIgnoreCase);

            foreach (string orphan in orphans)
            {
                string name = nodeNames.GetValueOrDefault(orphan, orphan);
                string prefix = GetNamePrefix(name);
                if (!prefixGroups.ContainsKey(prefix))
                    prefixGroups[prefix] = new List<string>();
                prefixGroups[prefix].Add(orphan);
            }

            foreach (var (_, group) in prefixGroups.OrderBy(kv => kv.Key))
            {
                blocks.Add((null, group, true));
            }
        }

        progress?.Report((40, "Positioning nodes..."));

        // Layout: two-level grid
        Dictionary<string, GraphLayoutPosition> positions = new();
        int outerCols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(blocks.Count)));
        double outerX = 0, outerY = 0;
        int col = 0;
        double rowMaxH = 0;

        foreach (var (regionTag, nodeIds, isOrphan) in blocks)
        {
            int n = nodeIds.Count;
            int innerCols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(n)));

            for (int i = 0; i < nodeIds.Count; i++)
            {
                int r = i / innerCols;
                int c = i % innerCols;
                positions[nodeIds[i]] = new GraphLayoutPosition
                {
                    X = outerX + c * nodeSpacing,
                    Y = outerY + r * nodeSpacing
                };
            }

            // Also position the region parent node (centered above its children)
            if (regionTag != null)
            {
                int innerRows = Math.Max(1, (int)Math.Ceiling((double)n / innerCols));
                positions["region_" + regionTag] = new GraphLayoutPosition
                {
                    X = outerX + (innerCols - 1) * nodeSpacing / 2.0,
                    Y = outerY + (innerRows - 1) * nodeSpacing / 2.0
                };
            }

            double blockW = innerCols * nodeSpacing;
            int blockRows = (int)Math.Ceiling((double)n / innerCols);
            double blockH = blockRows * nodeSpacing;
            rowMaxH = Math.Max(rowMaxH, blockH);

            double gap = isOrphan ? orphanGroupGap : regionGap;
            outerX += blockW + gap;
            col++;

            if (col >= outerCols)
            {
                col = 0;
                outerX = 0;
                outerY += rowMaxH + regionGap;
                rowMaxH = 0;
            }
        }

        progress?.Report((90, "Finalizing..."));
        return positions;
    }

    // ==================== Simple Grid Layout ====================

    private Dictionary<string, GraphLayoutPosition> ComputeGridLayout(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress)
    {
        progress?.Report((20, "Computing grid layout..."));

        List<AreaNodeDto> allNodes = graphData.Nodes.Concat(graphData.DisconnectedAreas).ToList();
        int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(allNodes.Count)));
        int spacing = GetConfigInt(config, "nodeSpacing", 40);

        Dictionary<string, GraphLayoutPosition> positions = new();
        for (int i = 0; i < allNodes.Count; i++)
        {
            int r = i / cols;
            int c = i % cols;
            positions[allNodes[i].ResRef] = new GraphLayoutPosition
            {
                X = c * spacing,
                Y = r * spacing
            };
        }

        // Add region parents at center of their children
        AddRegionParentPositions(positions, regions, graphData);

        progress?.Report((90, "Finalizing..."));
        return positions;
    }

    // ==================== Circle Layout ====================

    private Dictionary<string, GraphLayoutPosition> ComputeCircleLayout(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress)
    {
        progress?.Report((20, "Computing circle layout..."));

        List<AreaNodeDto> allNodes = graphData.Nodes.Concat(graphData.DisconnectedAreas).ToList();
        double radius = allNodes.Count * 8; // Scale radius with node count

        Dictionary<string, GraphLayoutPosition> positions = new();
        for (int i = 0; i < allNodes.Count; i++)
        {
            double angle = 2 * Math.PI * i / allNodes.Count;
            positions[allNodes[i].ResRef] = new GraphLayoutPosition
            {
                X = radius * Math.Cos(angle),
                Y = radius * Math.Sin(angle)
            };
        }

        AddRegionParentPositions(positions, regions, graphData);

        progress?.Report((90, "Finalizing..."));
        return positions;
    }

    // ==================== Concentric Layout ====================

    private Dictionary<string, GraphLayoutPosition> ComputeConcentricLayout(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        Dictionary<string, object>? config,
        IProgress<(int Percent, string Phase)>? progress)
    {
        progress?.Report((20, "Computing concentric layout..."));

        int levelWidth = GetConfigInt(config, "levelWidth", 2);
        int minNodeSpacing = GetConfigInt(config, "minNodeSpacing", 10);

        // Build adjacency for degree calculation
        Dictionary<string, int> degree = new(StringComparer.OrdinalIgnoreCase);
        List<AreaNodeDto> allNodes = graphData.Nodes.Concat(graphData.DisconnectedAreas).ToList();
        foreach (AreaNodeDto node in allNodes) degree[node.ResRef] = 0;
        foreach (AreaEdgeDto edge in graphData.Edges)
        {
            if (degree.ContainsKey(edge.SourceResRef)) degree[edge.SourceResRef]++;
            if (degree.ContainsKey(edge.TargetResRef)) degree[edge.TargetResRef]++;
        }

        // Sort by degree descending, group into concentric levels
        List<AreaNodeDto> sorted = allNodes.OrderByDescending(n => degree.GetValueOrDefault(n.ResRef, 0)).ToList();

        Dictionary<string, GraphLayoutPosition> positions = new();
        int level = 0;
        int idx = 0;

        while (idx < sorted.Count)
        {
            int count = Math.Max(1, levelWidth * (level + 1));
            count = Math.Min(count, sorted.Count - idx);
            double radius = level == 0 ? 0 : level * (minNodeSpacing + NodeWidth) * 2;

            for (int i = 0; i < count && idx < sorted.Count; i++, idx++)
            {
                double angle = count == 1 ? 0 : 2 * Math.PI * i / count;
                positions[sorted[idx].ResRef] = new GraphLayoutPosition
                {
                    X = radius * Math.Cos(angle),
                    Y = radius * Math.Sin(angle)
                };
            }

            level++;
        }

        AddRegionParentPositions(positions, regions, graphData);

        progress?.Report((90, "Finalizing..."));
        return positions;
    }

    // ==================== MSAGL Graph Building ====================

    private (GeometryGraph Graph, Dictionary<string, Node> NodeMap) BuildMsaglGraph(
        AreaGraphDto graphData,
        List<RegionInfo> regions)
    {
        var graph = new GeometryGraph();
        Dictionary<string, Node> nodeMap = new(StringComparer.OrdinalIgnoreCase);

        // Build region -> areas lookup
        Dictionary<string, HashSet<string>> regionAreaSets = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> areaToRegionTag = new(StringComparer.OrdinalIgnoreCase);

        foreach (RegionInfo region in regions)
        {
            regionAreaSets[region.Tag] = new HashSet<string>(region.AreaResRefs, StringComparer.OrdinalIgnoreCase);
            foreach (string resRef in region.AreaResRefs)
            {
                areaToRegionTag[resRef.ToLowerInvariant()] = region.Tag;
            }
        }

        // Create MSAGL nodes for all areas
        List<AreaNodeDto> allNodes = graphData.Nodes.Concat(graphData.DisconnectedAreas).ToList();
        foreach (AreaNodeDto nodeDto in allNodes)
        {
            ICurve curve = CurveFactory.CreateRectangle(NodeWidth, NodeHeight, new Point(0, 0));
            var node = new Node(curve, nodeDto.ResRef);
            graph.Nodes.Add(node);
            nodeMap[nodeDto.ResRef] = node;
        }

        // Create region clusters for compound node support
        Dictionary<string, Cluster> regionClusters = new(StringComparer.OrdinalIgnoreCase);
        foreach (RegionInfo region in regions)
        {
            if (!regionAreaSets.TryGetValue(region.Tag, out HashSet<string>? areaRefs) || areaRefs.Count == 0)
                continue;

            var cluster = new Cluster();
            cluster.UserData = "region_" + region.Tag;
            cluster.RectangularBoundary = new RectangularClusterBoundary
            {
                LeftMargin = RegionPadding,
                RightMargin = RegionPadding,
                TopMargin = RegionPadding,
                BottomMargin = RegionPadding
            };

            // Add area nodes to this cluster
            foreach (string resRef in areaRefs)
            {
                if (nodeMap.TryGetValue(resRef, out Node? areaNode))
                {
                    cluster.AddChild(areaNode);
                }
            }

            graph.RootCluster.AddChild(cluster);
            regionClusters[region.Tag] = cluster;

            // Also create a "label" node for the region parent (for position extraction)
            string regionNodeId = "region_" + region.Tag;
            ICurve regionCurve = CurveFactory.CreateRectangle(1, 1, new Point(0, 0));
            var regionNode = new Node(regionCurve, regionNodeId);
            nodeMap[regionNodeId] = regionNode;
            // Don't add to graph — we'll compute its position from the cluster bounding box
        }

        // Add orphan nodes to root cluster
        foreach (AreaNodeDto nodeDto in allNodes)
        {
            if (!areaToRegionTag.ContainsKey(nodeDto.ResRef.ToLowerInvariant()))
            {
                if (nodeMap.TryGetValue(nodeDto.ResRef, out Node? orphanNode))
                {
                    graph.RootCluster.AddChild(orphanNode);
                }
            }
        }

        // Create MSAGL edges
        foreach (AreaEdgeDto edgeDto in graphData.Edges)
        {
            if (nodeMap.TryGetValue(edgeDto.SourceResRef, out Node? source) &&
                nodeMap.TryGetValue(edgeDto.TargetResRef, out Node? target))
            {
                var edge = new Edge(source, target);
                graph.Edges.Add(edge);
            }
        }

        return (graph, nodeMap);
    }

    private Dictionary<string, GraphLayoutPosition> ExtractPositions(
        GeometryGraph graph,
        Dictionary<string, Node> nodeMap)
    {
        Dictionary<string, GraphLayoutPosition> positions = new();

        // Extract node positions from MSAGL result
        foreach (Node graphNode in graph.Nodes)
        {
            string? id = graphNode.UserData as string;
            if (id == null) continue;

            positions[id] = new GraphLayoutPosition
            {
                X = Math.Round(graphNode.Center.X, 2),
                Y = Math.Round(graphNode.Center.Y, 2)
            };
        }

        // Extract region parent positions from cluster bounding boxes
        ExtractClusterPositions(graph.RootCluster, positions);

        return positions;
    }

    private void ExtractClusterPositions(Cluster cluster, Dictionary<string, GraphLayoutPosition> positions)
    {
        foreach (Cluster child in cluster.Clusters)
        {
            if (child.UserData is string regionId)
            {
                // Position the region parent at the center of its cluster bounding box
                Rectangle bbox = child.BoundingBox;
                positions[regionId] = new GraphLayoutPosition
                {
                    X = Math.Round(bbox.Center.X, 2),
                    Y = Math.Round(bbox.Center.Y, 2)
                };
            }

            // Recurse for nested clusters
            ExtractClusterPositions(child, positions);
        }
    }

    // ==================== Helper: Add Region Parent Positions ====================

    private void AddRegionParentPositions(
        Dictionary<string, GraphLayoutPosition> positions,
        List<RegionInfo> regions,
        AreaGraphDto graphData)
    {
        foreach (RegionInfo region in regions)
        {
            List<GraphLayoutPosition> childPositions = region.AreaResRefs
                .Where(r => positions.ContainsKey(r))
                .Select(r => positions[r])
                .ToList();

            if (childPositions.Count > 0)
            {
                positions["region_" + region.Tag] = new GraphLayoutPosition
                {
                    X = Math.Round(childPositions.Average(p => p.X), 2),
                    Y = Math.Round(childPositions.Average(p => p.Y), 2)
                };
            }
        }
    }

    // ==================== Cache Key & Disk Persistence ====================

    private string BuildCacheKey(
        AreaGraphDto graphData,
        List<RegionInfo> regions,
        string algorithm,
        Dictionary<string, object>? config)
    {
        StringBuilder sb = new();
        sb.Append(algorithm);
        sb.Append('|');

        // Include config params in key
        if (config != null)
        {
            foreach (var (key, value) in config.OrderBy(kv => kv.Key))
            {
                sb.Append(key).Append('=').Append(value).Append(',');
            }
        }
        sb.Append('|');

        // Include node count and edge count as quick topology fingerprint
        sb.Append(graphData.Nodes.Count).Append(',');
        sb.Append(graphData.Edges.Count).Append(',');
        sb.Append(graphData.DisconnectedAreas.Count).Append('|');

        // Hash the full node/edge data for topology uniqueness
        foreach (AreaNodeDto node in graphData.Nodes.OrderBy(n => n.ResRef))
        {
            sb.Append(node.ResRef).Append(':').Append(node.Region ?? "").Append(';');
        }
        foreach (AreaEdgeDto edge in graphData.Edges.OrderBy(e => e.SourceResRef).ThenBy(e => e.TargetResRef))
        {
            sb.Append(edge.SourceResRef).Append('>').Append(edge.TargetResRef).Append(';');
        }

        // Region assignments affect compound layout
        foreach (RegionInfo region in regions.OrderBy(r => r.Tag))
        {
            sb.Append(region.Tag).Append('[');
            foreach (string resRef in region.AreaResRefs.OrderBy(r => r))
            {
                sb.Append(resRef).Append(',');
            }
            sb.Append(']');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..32];
    }

    private async Task<GraphLayoutResult?> LoadFromDiskCacheAsync(string cacheKey, CancellationToken ct)
    {
        string path = Path.Combine(_cacheDir, cacheKey + ".json");
        if (!File.Exists(path)) return null;

        try
        {
            string json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<GraphLayoutResult>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read disk cache: {Path}", path);
            return null;
        }
    }

    private async Task SaveToDiskCacheAsync(string cacheKey, GraphLayoutResult result)
    {
        try
        {
            string path = Path.Combine(_cacheDir, cacheKey + ".json");
            string json = JsonSerializer.Serialize(result, JsonOptions);
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to write disk cache for key: {Key}", cacheKey);
        }
    }

    // ==================== Config Helpers ====================

    private static int GetConfigInt(Dictionary<string, object>? config, string key, int defaultValue)
    {
        if (config == null || !config.TryGetValue(key, out object? val)) return defaultValue;
        return val switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            JsonElement je => je.TryGetInt32(out int i) ? i : defaultValue,
            string s => int.TryParse(s, out int i) ? i : defaultValue,
            _ => defaultValue
        };
    }

    private static double GetConfigDouble(Dictionary<string, object>? config, string key, double defaultValue)
    {
        if (config == null || !config.TryGetValue(key, out object? val)) return defaultValue;
        return val switch
        {
            double d => d,
            int i => i,
            long l => l,
            float f => f,
            JsonElement je => je.TryGetDouble(out double d) ? d : defaultValue,
            string s => double.TryParse(s, out double d) ? d : defaultValue,
            _ => defaultValue
        };
    }

    private static string GetConfigString(Dictionary<string, object>? config, string key, string defaultValue)
    {
        if (config == null || !config.TryGetValue(key, out object? val)) return defaultValue;
        return val switch
        {
            string s => s,
            JsonElement je => je.GetString() ?? defaultValue,
            _ => val.ToString() ?? defaultValue
        };
    }

    private static string GetNamePrefix(string name)
    {
        if (string.IsNullOrEmpty(name)) return "~";

        // Take first word or first 3 chars, whichever is shorter
        int spaceIdx = name.IndexOf(' ');
        if (spaceIdx > 0 && spaceIdx <= 10)
            return name[..spaceIdx].ToLowerInvariant();

        return name.Length <= 3 ? name.ToLowerInvariant() : name[..3].ToLowerInvariant();
    }
}

/// <summary>
/// Lightweight region info for layout computation (avoids coupling to full RegionDefinitionDto).
/// </summary>
public class RegionInfo
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> AreaResRefs { get; set; } = new();
}
