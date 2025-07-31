using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

public class NodeCreator(EconomySubsystem economy)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void SpawnNodeForType(ResourceType type, AreaDefinition areaDefinition, Location location)
    {
        switch (type)
        {
            case ResourceType.Undefined:
                Log.Error($"Undefined node in {areaDefinition.ResRef} rejected. Did not spawn.");
                break;
            case ResourceType.Ore:
                Log.Info("Spawning an Ore node");
                SpawnOreNode(areaDefinition, location, ResourceType.Ore);
                break;
            case ResourceType.Geode:
                Log.Info("Spawning a Geode node");
                SpawnOreNode(areaDefinition, location, ResourceType.Geode);
                break;
            case ResourceType.Boulder:
                break;
            case ResourceType.Tree:
                break;
            case ResourceType.Flora:
                break;
            default: break;
        }
    }

    private void SpawnOreNode(AreaDefinition areaDefinition, Location location, ResourceType resourceType)
    {
        List<ResourceNodeDefinition> defs = NodeDefinitionsForArea(areaDefinition, resourceType);

        // Generate a random index
        Random rng = new();
        int index = rng.Next(defs.Count);

        if (!location.IsValid)
        {
            Log.Error($"A node spawn location in {areaDefinition.ResRef} wasn't valid!");
            return;
        }

        NwPlaceable? plc = NwPlaceable.Create(WorldConfigConstants.GenericNodePlcRef, location);

        if (plc is null)
        {
            Log.Error($"PLC creation failed in {areaDefinition.ResRef}");
            return;
        }

        ResourceNodeDefinition resourceNodeDefinition = defs[index];

        ObjectPlugin.SetAppearance(plc, resourceNodeDefinition.Appearance);
        // Generate a random float for scale variance
        plc.Name = resourceNodeDefinition.Name;
        plc.Description = resourceNodeDefinition.Description;
        plc.Tag = resourceNodeDefinition.Tag;
        ResourceNodeInstance newInstance = new ResourceNodeInstance()
        {
            Definition = resourceNodeDefinition,
            Location = new SavedLocation
            {
                AreaResRef = areaDefinition.ResRef,
                X = plc.Location.Position.X,
                Y = plc.Location.Position.Y,
                Z = plc.Location.Position.Z,
                Orientation = rng.NextFloat(0, 360)
            },
            Quantity = resourceNodeDefinition.BaseQuantity,
            Scale = plc.VisualTransform.Scale
        };

        if (economy.PersistNode(newInstance))
        {
            economy.RegisterNode(plc, newInstance);
        }
    }

    private List<ResourceNodeDefinition> NodeDefinitionsForArea(AreaDefinition areaDefinition, ResourceType type)
    {
        List<ResourceNodeDefinition> oreDefinitions =
            economy.GetStoredDefinitions()
                .Where(d => d.Type == type && areaDefinition.SpawnableNodes.Contains(d.Tag))
                .ToList();
        return oreDefinitions;
    }
}
