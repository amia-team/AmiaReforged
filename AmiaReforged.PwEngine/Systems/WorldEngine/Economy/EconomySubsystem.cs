using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.HarvestActions;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomySubsystem))]
public class EconomySubsystem
{
    private readonly IWorldConfigProvider _config;
    private readonly NodeCreator _nodeCreator;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public EconomyDefinitions Definitions { get; }
    private EconomyPersistence Persistence { get; }

    private readonly Dictionary<Guid, ResourceNodeInstance> _nodeInstances = new();

    public EconomySubsystem(EconomyDefinitions definitions, EconomyPersistence persistence, IWorldConfigProvider config)
    {
        _config = config;
        _nodeCreator = new NodeCreator(this);
        Definitions = definitions;
        Persistence = persistence;

        UpdateStoredDefinitions();

        bool initialized = _config.GetBoolean(WorldConfigConstants.InitializedKey);
        if (!initialized)
        {
            Log.Info("----- DOING FIRST TIME SETUP -----");
            DoFirstTimeSetUp();
        }
        else
        {
            SpawnNodesFromDb();
        }
    }

    private void SpawnNodesFromDb()
    {
        const int batchSize = 50;
        int skip = 0;
        bool hasMore = true;

        while (hasMore)
        {
            List<ResourceNodeInstance> instances = Persistence.AllResourceNodes().Skip(skip).Take(batchSize).ToList();

            if (instances.Count == 0)
            {
                hasMore = false;
                continue;
            }

            foreach (ResourceNodeInstance instance in instances)
            {
                NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == instance.Location.AreaResRef);
                if (area == null)
                {
                    Log.Error($"Invalid area defined for {instance.Location.AreaResRef} with id {instance.Id}");
                    continue;
                }

                Location location = Location.Create(area, instance.Location.Position, instance.Location.Orientation);

                if (!location.IsValid)
                {
                    Log.Error($"Invalid location for resource node with id {instance.Id} in {area.ResRef}");
                    continue;
                }

                NwPlaceable? plc = NwPlaceable.Create(WorldConfigConstants.GenericNodePlcRef, location);
                if (plc == null)
                {
                    Log.Error($"Failed to generate PLC for {instance.Definition.Name} in {area.ResRef}");
                    continue;
                }

                plc.Name = instance.Definition.Name;
                plc.Tag = instance.Definition.Tag;
                plc.VisualTransform.Scale = instance.Scale;
                plc.Description = instance.Definition.Description;
                ObjectPlugin.SetAppearance(plc, instance.Definition.Appearance);

                RegisterNode(plc, instance);
            }

            skip += batchSize;
        }
    }

    private void DoFirstTimeSetUp()
    {
        Log.Info("Retrieving regions . . .");
        foreach (RegionDefinition region in Definitions.Regions)
        {
            Log.Info($"Found {region.Name}");
            Log.Info($"Number of areas defined: {region.Areas.Count}");

            InitializeAreaNodeSpawnZones(region);
        }

        Log.Info("All regions processed.");
        Log.Info("Setting initialized to true in config.");
        // _config.SetBoolean(WorldConfigConstants.InitializedKey, true);
    }

    private void InitializeAreaNodeSpawnZones(RegionDefinition region)
    {
        foreach (AreaDefinition areaDefinition in region.Areas)
        {
            Log.Info($"Processing area {areaDefinition.ResRef}");

            NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaDefinition.ResRef);
            if (area == null)
            {
                Log.Error($"Invalid area defined for {region.Name}: {areaDefinition.ResRef}");
                continue;
            }

            Log.Info("Area successfully retrieved.");
            Log.Info($"Getting node spawn zones from {area.Name}");

            int numberOfPointsInArea = area.FindObjectsOfTypeInArea<NwWaypoint>().Count();
            Log.Info($"There are {numberOfPointsInArea} in {area.Name}");
            List<NwTrigger> nodeSpawnTriggers = area.FindObjectsOfTypeInArea<NwTrigger>()
                .Where(t => t.Tag == WorldConfigConstants.ResourceNodeZoneTag).ToList();
            Log.Info($"Found {nodeSpawnTriggers.Count} node spawn zones");


            ProcessWaypointsAndSpawnNodes(nodeSpawnTriggers, area, areaDefinition);
        }
    }

    private void ProcessWaypointsAndSpawnNodes(List<NwTrigger> nodeSpawnTriggers, NwArea area, AreaDefinition areaDefinition)
    {
        foreach (NwTrigger trigger in nodeSpawnTriggers)
        {
            LocalVariableString v = trigger.GetObjectVariable<LocalVariableString>("node_tags");
            if (v.Value.IsNullOrEmpty())
            {
                Log.Error($"No node tags found for {trigger.ResRef} in {area.ResRef}");
                continue;
            }

            Log.Info($"Tags for zone are: {v.Value}");
            string[] tags = v.Value!.Split(',');

            List<Location> locations = ExtractWaypointsLocations(trigger);

            // Create a list of available tag indices and shuffle them
            List<int> availableTagIndices = new();
            for (int i = 0; i < tags.Length; i++)
            {
                availableTagIndices.Add(i);
            }

            Random rng = new();
            // Fisher-Yates shuffle
            for (int i = availableTagIndices.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (availableTagIndices[i], availableTagIndices[j]) =
                    (availableTagIndices[j], availableTagIndices[i]);
            }

            // Dictionary to keep track of how many nodes we've spawned per tag
            Dictionary<int, int> nodesPerTag = new();

            // Process each waypoint
            int currentIndex = 0;
            while (currentIndex < locations.Count)
            {
                // Try to find an available tag that hasn't reached its limit
                bool tagFound = false;
                foreach (int tagIndex in availableTagIndices)
                {
                    if (!nodesPerTag.ContainsKey(tagIndex))
                    {
                        nodesPerTag[tagIndex] = 0;
                    }

                    if (nodesPerTag[tagIndex] < 2) // Maximum 2 nodes per tag
                    {
                        ResourceType tagType = TagToType(tags[tagIndex]);
                        _nodeCreator.SpawnNodeForType(tagType, areaDefinition, locations[currentIndex]);
                        nodesPerTag[tagIndex]++;
                        currentIndex++;
                        tagFound = true;
                        break;
                    }
                }

                // If no available tags found (all at limit or no more tags), break
                if (!tagFound || currentIndex >= locations.Count)
                {
                    break;
                }
            }
        }
    }

    private static List<Location> ExtractWaypointsLocations(NwTrigger trigger)
    {
        List<Location> locations = [];
        uint currentWp = NWScript.GetFirstInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
        while (NWScript.GetIsObjectValid(currentWp) == NWScript.TRUE)
        {
            Log.Info($"Current WP is {currentWp}");
            if (NWScript.GetResRef(currentWp) != WorldConfigConstants.NodeSpawnPointRef)
            {
                Log.Info("Waypoint not a spawn node, skipping");
                currentWp = NWScript.GetNextInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
                continue;
            }

            NwWaypoint? nwWaypoint = currentWp.ToNwObject<NwWaypoint>();
            if (nwWaypoint != null)
            {
                Location? l = nwWaypoint.Location;
                if (l != null) locations.Add(l);
            }

            currentWp = NWScript.GetNextInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
            Log.Info("Getting next waypoint.");
        }

        return locations;
    }

    private ResourceType TagToType(string tag)
    {
        return tag switch
        {
            "ore" => ResourceType.Ore,
            "geode" => ResourceType.Geode,
            "tree" => ResourceType.Tree,
            "boulder" => ResourceType.Boulder,
            "flora" => ResourceType.Flora,
            _ => ResourceType.Undefined
        };
    }

    private void UpdateStoredDefinitions()
    {
        Persistence.UpdateDefinitions(Definitions);
    }


    public bool PersistNode(ResourceNodeInstance resourceNodeInstance)
    {
        return Persistence.StoreNewNode(resourceNodeInstance);
    }

    public List<ResourceNodeDefinition> GetStoredDefinitions()
    {
        return Persistence.AllResourceDefinitions();
    }

    public void RegisterNode(NwPlaceable nodePlc, ResourceNodeInstance instance)
    {
        bool success = _nodeInstances.TryAdd(nodePlc.UUID, instance);
        if (!success)
        {
            Log.Error("Failed to register instance: UUID collision occurred...");
            return;
        }

        RegisterPlcEvents(nodePlc, instance);
        Log.Info("Node registered");
    }

    private void RegisterPlcEvents(NwPlaceable plc, ResourceNodeInstance instance)
    {
        switch (instance.Definition.HarvestAction)
        {
            case HarvestActionEnum.Undefined:
                Log.Error($"Invalid harvest for node {plc.Area?.Name}. Event not subscribed.");
                break;
            case HarvestActionEnum.Attack:
                plc.OnPhysicalAttacked += HarvestAttackableNode;
                plc.OnUsed += RedirectToAttack;
                break;
            case HarvestActionEnum.Use:
                plc.OnUsed += HarvestUsableNode;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HarvestAttackableNode(PlaceableEvents.OnPhysicalAttacked obj)
    {
        if (obj.Attacker == null)
        {
            return;
        }

        NwItem? mainHand = obj.WeaponUsed(obj.Attacker);

        if (mainHand == null)
        {
            obj.Placeable.SpeakString("*Ore cannot be harvested using your bare hands*");
            obj.Attacker.ApplyEffect(EffectDuration.Instant, Effect.Damage(1, DamageType.Bludgeoning));
            return;
        }

        if (mainHand.BaseItem.ItemType != BaseItemType.Warhammer)
        {
            obj.Placeable.SpeakString("*This ore requires a pick to mine*");
        }

        obj.Placeable.SpeakString($"{_nodeInstances[obj.Placeable.UUID].Quantity} {obj.Placeable.Name} harvested!");
        obj.Placeable.SpeakString($"{_nodeInstances[obj.Placeable.UUID].Definition.YieldItems.First().ItemTag} ");
    }

    private void RedirectToAttack(PlaceableEvents.OnUsed obj)
    {
        obj.UsedBy.ActionAttackTarget(obj.Placeable);
    }

    private void HarvestUsableNode(PlaceableEvents.OnUsed obj)
    {
        obj.Placeable.SpeakString($"{obj.Placeable.Name} harvested");
    }
}
