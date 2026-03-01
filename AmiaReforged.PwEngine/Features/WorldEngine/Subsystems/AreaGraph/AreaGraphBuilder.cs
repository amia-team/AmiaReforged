using Anvil;
using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.Encounters.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;

/// <summary>
/// Builds an <see cref="AreaGraphData"/> by scanning all areas in the module
/// and discovering transitions via doors and triggers.
/// </summary>
public class AreaGraphBuilder
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Scan every area in the running module and build the connectivity graph.
    /// Switches to the main NWN server thread for all VM calls.
    /// </summary>
    public async Task<AreaGraphData> BuildAsync()
    {
        Log.Info("Building area graph...");

        // All NWN API calls (Areas, Objects, TransitionTarget) must run on the main thread
        await NwTask.SwitchToMainThread();

        var allAreas = NwModule.Instance.Areas.ToList();
        Log.Info("Found {Count} areas in module", allAreas.Count);

        // Load spawn profile lookup (area resref -> profile name)
        Dictionary<string, string> spawnProfileNames = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var profileRepo = AnvilCore.GetService<ISpawnProfileRepository>();
            if (profileRepo != null)
            {
                var profiles = await profileRepo.GetAllAsync();
                foreach (var profile in profiles)
                {
                    if (!string.IsNullOrWhiteSpace(profile.AreaResRef))
                    {
                        spawnProfileNames.TryAdd(profile.AreaResRef, profile.Name);
                    }
                }

                // Re-enter main thread after the async DB call
                await NwTask.SwitchToMainThread();

                Log.Info("Loaded {Count} spawn profiles for area graph", spawnProfileNames.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to load spawn profiles for area graph, continuing without them");
            // Re-enter main thread in case we left it
            await NwTask.SwitchToMainThread();
        }

        // Build a lookup of ResRef -> AreaNode for all areas
        Dictionary<string, AreaNode> nodesByResRef = new();
        foreach (NwArea area in allAreas)
        {
            string resRef = area.ResRef;
            if (string.IsNullOrWhiteSpace(resRef)) continue;

            bool hasProfile = spawnProfileNames.ContainsKey(resRef);
            string? profileName = hasProfile ? spawnProfileNames[resRef] : null;

            // If duplicate resrefs exist, keep the first one
            nodesByResRef.TryAdd(resRef, new AreaNode(resRef, area.Name,
                HasSpawnProfile: hasProfile, SpawnProfileName: profileName));
        }

        // Discover all transition edges
        List<AreaEdge> edges = [];
        HashSet<string> connectedResRefs = [];

        foreach (NwArea area in allAreas)
        {
            string sourceResRef = area.ResRef;
            if (string.IsNullOrWhiteSpace(sourceResRef)) continue;

            // Scan doors for transitions
            foreach (NwDoor door in area.Objects.OfType<NwDoor>())
            {
                NwArea? targetArea = door.TransitionTarget?.Area;
                if (targetArea == null) continue;

                string targetResRef = targetArea.ResRef;
                if (string.IsNullOrWhiteSpace(targetResRef)) continue;
                if (targetResRef == sourceResRef) continue; // skip self-loops

                edges.Add(new AreaEdge(sourceResRef, targetResRef, TransitionType.Door, door.Tag ?? ""));
                connectedResRefs.Add(sourceResRef);
                connectedResRefs.Add(targetResRef);
            }

            // Scan triggers for transitions
            foreach (NwTrigger trigger in area.Objects.OfType<NwTrigger>())
            {
                NwArea? targetArea = trigger.TransitionTarget?.Area;
                if (targetArea == null) continue;

                string targetResRef = targetArea.ResRef;
                if (string.IsNullOrWhiteSpace(targetResRef)) continue;
                if (targetResRef == sourceResRef) continue;

                edges.Add(new AreaEdge(sourceResRef, targetResRef, TransitionType.Trigger, trigger.Tag ?? ""));
                connectedResRefs.Add(sourceResRef);
                connectedResRefs.Add(targetResRef);
            }
        }

        // Partition nodes into connected vs disconnected
        List<AreaNode> connectedNodes = [];
        List<AreaNode> disconnectedNodes = [];

        foreach (var node in nodesByResRef.Values)
        {
            if (connectedResRefs.Contains(node.ResRef))
            {
                connectedNodes.Add(node);
            }
            else
            {
                disconnectedNodes.Add(node);
            }
        }

        // Sort for deterministic output
        connectedNodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        disconnectedNodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        edges.Sort((a, b) =>
        {
            int cmp = string.Compare(a.SourceResRef, b.SourceResRef, StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : string.Compare(a.TargetResRef, b.TargetResRef, StringComparison.OrdinalIgnoreCase);
        });

        var graph = new AreaGraphData
        {
            Nodes = connectedNodes,
            Edges = edges,
            DisconnectedAreas = disconnectedNodes,
            GeneratedAtUtc = DateTime.UtcNow
        };

        Log.Info(
            "Area graph built: {Connected} connected areas, {Disconnected} disconnected areas, {Edges} edges",
            connectedNodes.Count, disconnectedNodes.Count, edges.Count);

        return graph;
    }
}