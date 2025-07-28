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

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomySubsystem))]
public class EconomySubsystem
{
    private readonly NodeSeeder _nodeSeeder;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public EconomyDefinitions Definitions { get; }
    private EconomyPersistence Persistence { get; }

    private Dictionary<Guid, ResourceNodeInstance> _nodeInstances = new();

    public EconomySubsystem(EconomyDefinitions definitions, EconomyPersistence persistence, IWorldConfigProvider config,
        NodeSeeder nodeSeeder)
    {
        _nodeSeeder = nodeSeeder;
        Definitions = definitions;
        Persistence = persistence;

        UpdateStoredDefinitions();

        bool initialized = config.GetBoolean(WorldConfigConstants.InitializedKey);
        if (!initialized)
        {
            DoFirstTimeSetUp();
        }
        else
        {
            SpawnNodesFromDb();
        }
    }

    private void SpawnNodesFromDb()
    {
        List<ResourceNodeInstance> instances = Persistence.AllResourceNodes();
    }

    private void DoFirstTimeSetUp()
    {
        foreach (RegionDefinition region in Definitions.Regions)
        {
            foreach (AreaDefinition areaDefinition in region.Areas)
            {
                NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaDefinition.ResRef);
                if (area == null)
                {
                    Log.Error($"Invalid area defined for {region.Name}: {areaDefinition.ResRef}");
                    continue;
                }

                IEnumerable<NwTrigger> nodeSpawnTriggers = area.FindObjectsOfTypeInArea<NwTrigger>()
                    .Where(t => t.Tag == WorldConfigConstants.ResourceNodeZoneTag);

                foreach (NwTrigger trigger in nodeSpawnTriggers)
                {
                    LocalVariableString v = trigger.GetObjectVariable<LocalVariableString>("node_tags");

                    if (v.Value.IsNullOrEmpty())
                    {
                        Log.Error($"No node tags found for {trigger.ResRef} in {area.ResRef}");
                        continue;
                    }

                    string[] tags = v.Value!.Split(',');
                    List<NwWaypoint> usedWaypoints = [];

                    foreach (string tag in tags)
                    {
                        uint currentWp = NWScript.GetFirstInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);

                        while (NWScript.GetIsObjectValid(currentWp) == NWScript.TRUE)
                        {
                            if (usedWaypoints.Any(w => w == currentWp) || NWScript.GetResRef(currentWp) !=
                                WorldConfigConstants.NodeSpawnPointRef)
                            {
                                currentWp = NWScript.GetNextInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
                                continue;
                            }

                            ResourceType tagType = TagToType(tag);

                            _nodeSeeder.SpawnNodeForType(tagType, areaDefinition, NWScript.GetLocation(currentWp)!);
                        }
                    }
                }
            }
        }
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
        return Persistence.GetStoredDefinitions();
    }

    public void RegisterNode(NwPlaceable nodePlc, ResourceNodeInstance instance)
    {
        _nodeInstances.TryAdd(nodePlc.UUID, instance);

        RegisterPlcEvents(nodePlc, instance);
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
            obj.Placeable.SpeakString("*This node cannot be harvested using your bare hands*", TalkVolume.Whisper);
        }
    }

    private void RedirectToAttack(PlaceableEvents.OnUsed obj)
    {
        obj.UsedBy.ActionAttackTarget(obj.Placeable);
    }

    private void HarvestUsableNode(PlaceableEvents.OnUsed obj)
    {
        // TODO: Implement
    }
}
