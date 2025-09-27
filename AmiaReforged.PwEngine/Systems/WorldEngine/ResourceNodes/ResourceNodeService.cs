using System.Numerics;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Domains;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

[ServiceBinding(typeof(ResourceNodeService))]
public class ResourceNodeService(RuntimeNodeService runtimeNodes)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ResourceNodeInstance CreateNewNode(AreaDefinition area, ResourceNodeDefinition definition, Vector3 position,
        float rotation = 0f)
    {
        IPQuality quality = (IPQuality)definition.GetQualityForArea(area);

        NwModule.Instance.SendMessageToAllDMs($"Quality for {definition.Name} was {quality}");

        int usesModifier = (int)quality < (int)IPQuality.Average
            ? (int)quality * -1
            : (int)quality;
        ResourceNodeInstance node = new()
        {
            Area = area.ResRef,
            Definition = definition,
            Uses = definition.Uses + usesModifier,
            Quality = quality,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Rotation = rotation
        };

        return node;
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
                Log.Info($"Registering new node with UUID {node.Id}");
                plc.Name =
                    $"{QualityLabel.QualityLabelForNode(node.Definition.Type, node.Quality)} {node.Definition.Name}";
                plc.Description = node.Definition.Description;

                runtimeNodes.RegisterPlaceable(plc, node);
            }
        }
    }
}
