using AmiaReforged.Core.Models;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.ResourceNodes;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface ISubSystemInitializer
{
    void Init(EconomySubsystem economySubsystem);
}

[ServiceBinding(typeof(ISubSystemInitializer))]
public class OreNodeInitializer : ISubSystemInitializer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void Init(EconomySubsystem economySubsystem)
    {
        foreach (RegionDefinition region in economySubsystem.Regions)
        {
            SetUpArea(economySubsystem, region);
        }
    }

    private static void SetUpArea(EconomySubsystem economySubsystem, RegionDefinition region)
    {
        foreach (AreaDefinition area in region.Areas)
        {
            NwArea? gameArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == area.ResRef);

            List<ResourceNodeDefinition> ores = GetOresForArea(economySubsystem, area);

            if (gameArea == null)
            {
                Log.Error($"Invalid resref for {region.Name}: {area.ResRef}");
                continue;
            }

            IEnumerable<NwTrigger> triggers = gameArea.FindObjectsOfTypeInArea<NwTrigger>()
                .Where(t => t.ResRef == WorldConfigConstants.ResourceNodeZoneRef);

            SetUpZone(economySubsystem, triggers, area, ores);
        }
    }

    private static List<ResourceNodeDefinition> GetOresForArea(EconomySubsystem economySubsystem, AreaDefinition area)
    {
        List<ResourceNodeDefinition> oreDefinitions =
            economySubsystem.NodeDefinitions.Where(n => n.Type == ResourceType.Ore).ToList();
        List<ResourceNodeDefinition> intersection = [];
        intersection.AddRange(area.SpawnableNodes
            .Select(nodeType => oreDefinitions.FirstOrDefault(n => n.Tag == nodeType))
            .OfType<ResourceNodeDefinition>());
        return intersection;
    }

    private static void SetUpZone(EconomySubsystem economySubsystem, IEnumerable<NwTrigger> triggers, AreaDefinition area,
        List<ResourceNodeDefinition> ores)
    {
        foreach (NwTrigger trigger in triggers)
        {
            LocalVariableString v = trigger.GetObjectVariable<LocalVariableString>("node_tags");

            if (v.HasNothing)
            {
                Log.Error($"No node tags found for {trigger.ResRef} in {area.ResRef}");
                continue;
            }

            string[] tags = v.Value!.Split(',');

            if (tags.All(s => s != "ore")) continue;


            uint current = NWScript.GetFirstInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);

            while (NWScript.GetIsObjectValid(current) == NWScript.TRUE)
            {
                if (NWScript.GetTag(current) == WorldConfigConstants.NodeSpawnPointRef)
                {
                    NwPlaceable? plc = NwPlaceable.Create(WorldConfigConstants.OrePlcRef,
                        NWScript.GetLocation(current)!);

                    if (plc != null)
                    {
                        Random rng = new();
                        ResourceNodeDefinition randomNode = ores[rng.Next(ores.Count)];
                        ObjectPlugin.SetAppearance(plc, randomNode.Appearance);

                        economySubsystem.ResourceInstances.Add(new ResourceNodeInstance
                        {
                            Definition = randomNode,
                            Location = new SavedLocation
                            {
                                AreaResRef = plc.Area!.ResRef,
                                X = plc.Location.Position.X,
                                Y = plc.Location.Position.Y,
                                Z = plc.Location.Position.Z,
                                Orientation = plc.Rotation
                            },
                            Richness = new Random().NextFloat(0.1f, 1.0f),
                            Instance = plc
                        });
                    }
                }

                current = NWScript.GetNextInPersistentObject(trigger, NWScript.OBJECT_TYPE_WAYPOINT);
            }
        }
    }
}

public enum ResourceType
{
    Ore,
    Geode,
    Boulder,
    Tree,
    Flora
}

[ServiceBinding(typeof(ISubSystemInitializer))]
public class TreeNodeInitializer : ISubSystemInitializer
{
    public void Init(EconomySubsystem economySubsystem)
    {
    }
}
