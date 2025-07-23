using AmiaReforged.Core.Models;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(ResourceNodeFactory))]
public class ResourceNodeFactory(EconomySubsystem economy)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void CreateOre(Location l, ResourceNodeDefinition definition)
    {
        if (definition.Type != ResourceType.Ore)
        {
            Log.Error(
                $"Failed to generate an Ore node in {l.Area.Name}: Node Definition was {definition.Type}, expected {ResourceType.Ore}");
            return;
        }

        NwPlaceable? plc = NwPlaceable.Create(WorldConfigConstants.GenericNodePlcRef, l);

        if (plc == null)
        {
            Log.Error($"Failed to generate an Ore node in {l.Area.Name}: Resultant PLC instance was null.");
            return;
        }

        ResourceNodeInstance resourceNodeInstance = new ResourceNodeInstance
        {
            Definition = definition,
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
        };

        economy.PersistNode(resourceNodeInstance);
    }
}
