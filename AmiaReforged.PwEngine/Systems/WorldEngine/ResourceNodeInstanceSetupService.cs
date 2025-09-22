using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Domains;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(ResourceNodeInstanceSetupService))]
public class ResourceNodeInstanceSetupService(
    IResourceNodeDefinitionRepository resourceRepository,
    IHarvestProcessor harvestProcessor,
    IRegionRepository regionRepository,
    RuntimeNodeService runtimeNodes)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void DoSetup()
    {
        NwModule.Instance.SendMessageToAllDMs("INITIALIZING ECONOMY . . .");
        ClearOldNodes();
        foreach (RegionDefinition region in regionRepository.All())
        {
            foreach (AreaDefinition area in region.Areas)
            {
                GenerateNodes(area);
            }
        }
    }

    public void ClearOldNodes()
    {
        foreach (NwArea area in NwModule.Instance.Areas)
        {
            List<ResourceNodeInstance> instancesInArea = harvestProcessor.GetInstancesForArea(area.ResRef);

            if (instancesInArea.Count == 0) continue;

            NwModule.Instance.SendMessageToAllDMs($"PURGING NODES IN {area.Name} . . .");

            foreach (ResourceNodeInstance ri in instancesInArea)
            {
                ri.Destroy();
            }
        }
    }

    private void GenerateNodes(AreaDefinition area)
    {
        NwArea? nwArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == area.ResRef);

        if (nwArea is null)
        {
            Log.Error($"Area {area.ResRef} not found in module");
            return;
        }

        NwModule.Instance.SendMessageToAllDMs($"Processing {nwArea.Name} . . .");


        List<NwTrigger> nodeSpawnRegions =
            nwArea
                .FindObjectsOfTypeInArea<NwTrigger>()
                .Where(t => t.Tag == WorldConstants.ResourceNodeZoneTag)
                .ToList();

        GenerateNodesInTriggers(nodeSpawnRegions, area.DefinitionTags);
    }

    private void GenerateNodesInTriggers(List<NwTrigger> nodeSpawnRegions, List<string> definitionTags)
    {
        foreach (NwTrigger trigger in nodeSpawnRegions)
        {
            ProcessNodesToSpawn(trigger, definitionTags);
        }
    }

    private void ProcessNodesToSpawn(NwTrigger trigger, List<string> definitionTags)
    {
        if (definitionTags.Count == 0) return;

        Random rng = Random.Shared;

        ResourceType[] tags = GetTags(trigger);
        NwModule.Instance.SendMessageToAllDMs($"Number of tags for trigger: {tags.Length} . . .");

        foreach (ResourceType tag in tags)
        {
            NwModule.Instance.SendMessageToAllDMs($"trigger has tag: {tag} . . .");
        }


        List<NwWaypoint> waypoints = GetWaypoints(trigger);

        if (waypoints.Count == 0) return;

        List<ResourceNodeDefinition> definitions = GetDefinitions(definitionTags);
        NwModule.Instance.SendMessageToAllDMs($"Found {definitions.Count} definitions for {trigger.Area?.Name} . . .");

        foreach (ResourceNodeDefinition df in definitions)
        {
            NwModule.Instance.SendMessageToAllDMs($"Definition tag: {df.Type} . . .");
        }

        // Only keep resource types that have at least one matching definition
        List<ResourceType> typeOrder = GetTypeOrder(tags, definitions);
        NwModule.Instance.SendMessageToAllDMs($"Number of node types: {typeOrder.Count} . . .");

        if (typeOrder.Count == 0) return;

        NwModule.Instance.SendMessageToAllDMs($"Distributing nodes for trigger in {trigger.Area?.Name} . . .");

        DistributeNodes(typeOrder, rng, waypoints, definitions);
    }

    private void DistributeNodes(List<ResourceType> typeOrder, Random rng, List<NwWaypoint> waypoints,
        List<ResourceNodeDefinition> definitions)
    {
        // Shuffle types and waypoints to spread things out fairly
        Shuffle(typeOrder, rng);

        List<NwWaypoint> available = new(waypoints);
        Shuffle(available, rng);

        // Cap at most 2 per resource type
        Dictionary<ResourceType, int> remainingPerType = typeOrder.ToDictionary(t => t, _ => 2);

        HashSet<NwWaypoint> visited = new HashSet<NwWaypoint>();
        int wpIndex = 0;

        for (int round = 0; round < 2 && wpIndex < available.Count; round++)
        {
            foreach (ResourceType type in typeOrder)
            {
                if (wpIndex >= available.Count) break;
                if (remainingPerType[type] == 0) continue;

                List<ResourceNodeDefinition> matching = definitions.Where(d => d.Type == type).ToList();
                if (matching.Count == 0)
                {
                    remainingPerType[type] = 0;
                    continue;
                }

                NwWaypoint wp = available[wpIndex];

                // Ignore already-visited waypoints within this trigger pass
                if (visited.Contains(wp))
                {
                    wpIndex++;
                    continue;
                }

                ResourceNodeDefinition definition = matching[rng.Next(matching.Count)];

                NwModule.Instance.SendMessageToAllDMs($"Attempting to spawn a {definition.Tag} . . .");


                IPQuality baselineQuality =
                    (IPQuality)Random.Shared.Next((int)IPQuality.Poor, (int)IPQuality.AboveAverage);

                int usesModifier = (int)baselineQuality < (int)IPQuality.Average
                    ? (int)baselineQuality * -1
                    : (int)baselineQuality;

                ResourceNodeInstance node = new()
                {
                    Area = wp.Area!.ResRef,
                    Definition = definition,
                    Quality = baselineQuality,
                    Uses = definition.Uses + usesModifier,
                    X = wp.Position.X,
                    Y = wp.Position.Y,
                    Z = wp.Position.Z,
                    Rotation = wp.Rotation
                };

                SpawnInstance(node);

                visited.Add(wp);
                wpIndex++;
                remainingPerType[type]--;
            }
        }

        return;

        // Local helper to shuffle a list in-place
        static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public void SpawnInstance(ResourceNodeInstance node)
    {
        Location? l = node.GameLocation();

        if (l is null)
        {
            Log.Error($"Failed to get game location for node {node.Id}");
        }
        else
        {
            NwPlaceable? plc =
                NwPlaceable.Create(WorldConstants.GenericNodePlcRef, l, false, node.Definition.Tag);
            if (plc is null)
            {
                Log.Error($"Failed to create node {node.Id}");
            }
            else
            {
                ObjectPlugin.SetAppearance(plc, node.Definition.PlcAppearance);
                ObjectPlugin.ForceAssignUUID(plc, node.Id.ToUUIDString());
                plc.Name = $"{QualityLabel.ToQualityLabel((int)node.Quality)} {node.Definition.Name}";
                plc.Description = node.Definition.Description;

                runtimeNodes.RegisterPlaceable(plc, node);
            }
        }
    }

    private static List<ResourceType> GetTypeOrder(ResourceType[] tags, List<ResourceNodeDefinition> definitions)
    {
        List<ResourceType> typeOrder = tags
            .Distinct()
            .Where(t => definitions.Any(d => d.Type == t))
            .ToList();
        return typeOrder;
    }

    private List<ResourceNodeDefinition> GetDefinitions(List<string> definitionTags)
    {
        List<ResourceNodeDefinition> definitions =
            resourceRepository.All().Where(d => definitionTags.Contains(d.Tag)).ToList();
        return definitions;
    }

    private static List<NwWaypoint> GetWaypoints(NwTrigger trigger)
    {
        List<NwWaypoint> waypoints = trigger.GetObjectsInTrigger<NwWaypoint>()
            .Where(w => w.Tag == WorldConstants.NodeSpawnPointRef)
            .ToList();
        return waypoints;
    }

    private ResourceType[] GetTags(NwTrigger trigger)
    {
        ResourceType[] tags = NWScript.GetLocalString(trigger, WorldConstants.LvarNodeTags)
            .ToLower()
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(TagToEnum)
            .Where(t => t != ResourceType.Undefined)
            .ToArray();
        return tags;
    }


    private ResourceType TagToEnum(string tag)
    {
        return tag switch
        {
            "ore" => ResourceType.Ore,
            "tree" => ResourceType.Tree,
            "geode" => ResourceType.Geode,
            "boulder" => ResourceType.Boulder,
            "flora" => ResourceType.Flora,
            _ => ResourceType.Undefined
        };
    }
}
