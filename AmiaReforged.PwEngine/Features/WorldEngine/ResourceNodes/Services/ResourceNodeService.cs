using System.Numerics;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Services;

[ServiceBinding(typeof(ResourceNodeService))]
public class ResourceNodeService(
    RuntimeNodeService runtimeNodes,
    ICommandHandler<RegisterNodeCommand> registerNodeCommandHandler,
    IResourceNodeInstanceRepository nodeRepository)
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

        // Create RegisterNodeCommand to handle instance creation
        RegisterNodeCommand command = new RegisterNodeCommand(
            null, // New node, no existing ID
            definition.Tag,
            area.ResRef,
            position.X,
            position.Y,
            position.Z,
            rotation,
            quality,
            definition.Uses + usesModifier
        );

        // Execute command synchronously to get the created instance
        CommandResult result = registerNodeCommandHandler.HandleAsync(command).GetAwaiter().GetResult();

        if (!result.Success)
        {
            Log.Error($"Failed to create node: {result.ErrorMessage}");
            throw new InvalidOperationException($"Failed to create node: {result.ErrorMessage}");
        }

        Guid nodeId = (Guid)result.Data["nodeInstanceId"]!;

        // Retrieve the created instance from repository
        ResourceNodeInstance? node = nodeRepository.GetInstances().FirstOrDefault(n => n.Id == nodeId);

        if (node == null)
        {
            Log.Error($"Failed to retrieve created node {nodeId}");
            throw new InvalidOperationException($"Failed to retrieve created node {nodeId}");
        }

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
