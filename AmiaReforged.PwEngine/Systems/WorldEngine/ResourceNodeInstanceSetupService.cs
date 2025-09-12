using AmiaReforged.PwEngine.Systems.WorldEngine.Domains;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(ResourceNodeInstanceSetupService))]
public class ResourceNodeInstanceSetupService(
    IWorldConfigProvider configProvider,
    IResourceNodeDefinitionRepository resourceRepository,
    IResourceNodeInstanceRepository nodeRepository,
    IRegionRepository regionRepository)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void DoSetup()
    {
        ClearOldNodes();
        foreach (RegionDefinition region in regionRepository.All())
        {
            foreach (AreaDefinition area in region.Areas)
            {
                GenerateNodes(area);
            }
        }
    }

    private void ClearOldNodes()
    {
        foreach (NwArea area in NwModule.Instance.Areas)
        {
            List<ResourceNodeInstance> instancesInArea = nodeRepository.GetInstancesByArea(area.ResRef);

            if (instancesInArea.Count == 0) continue;

            foreach (ResourceNodeInstance ri in instancesInArea)
            {
                nodeRepository.RemoveNodeInstance(ri);
            }

            List<NwPlaceable> nodePlcs = area.FindObjectsOfTypeInArea<NwPlaceable>()
                .Where(p => p.ResRef == WorldConstants.GenericNodePlcRef).ToList();

            foreach (NwPlaceable plc in nodePlcs)
            {
                plc.Destroy();
            }

            nodeRepository.SaveChanges();
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
            ProcessNodesInTrigger(trigger, definitionTags);
        }
    }

    private void ProcessNodesInTrigger(NwTrigger trigger, List<string> definitionTags)
    {
        if (definitionTags.Count == 0) return;

        Random rng = Random.Shared;

        ResourceType[] tags = GetTags(trigger);

        List<NwWaypoint> waypoints = GetWaypoints(trigger);

        if (waypoints.Count == 0) return;

        List<ResourceNodeDefinition> definitions = GetDefinitions(definitionTags);

        // Only keep resource types that have at least one matching definition
        List<ResourceType> typeOrder = GetTypeOrder(tags, definitions);

        if (typeOrder.Count == 0) return;

        Distribute(typeOrder, rng, waypoints, definitions);
    }

    private void Distribute(List<ResourceType> typeOrder, Random rng, List<NwWaypoint> waypoints,
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

        // Two rounds → at most 2 per type
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

                SpawnResourceNode(definition, wp);

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

    private void SpawnResourceNode(ResourceNodeDefinition definition, NwWaypoint wp)
    {
        IPQuality baselineQuality = (IPQuality)Random.Shared.Next((int)IPQuality.Poor, (int)IPQuality.AboveAverage);

        int usesModifier = (int) baselineQuality < (int) IPQuality.Average ? (int) baselineQuality * -1 : (int) baselineQuality;

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
