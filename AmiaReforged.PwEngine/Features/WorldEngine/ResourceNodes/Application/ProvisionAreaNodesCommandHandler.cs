using System.Numerics;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Application;

[ServiceBinding(typeof(ICommandHandler<ProvisionAreaNodesCommand>))]
public class ProvisionAreaNodesCommandHandler : ICommandHandler<ProvisionAreaNodesCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ResourceNodeService _nodeService;
    private readonly IResourceNodeInstanceRepository _nodeRepository;
    private readonly IResourceNodeDefinitionRepository _definitionRepository;
    private readonly IEventBus _eventBus;
    private readonly Random _random = new();

    public ProvisionAreaNodesCommandHandler(
        ResourceNodeService nodeService,
        IResourceNodeInstanceRepository nodeRepository,
        IResourceNodeDefinitionRepository definitionRepository,
        IEventBus eventBus)
    {
        _nodeService = nodeService;
        _nodeRepository = nodeRepository;
        _definitionRepository = definitionRepository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(ProvisionAreaNodesCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var area = command.AreaDefinition;
            var provisionedNodes = new List<ResourceNodeInstance>();

            // Check if area already has nodes (unless forcing respawn)
            if (!command.ForceRespawn)
            {
                var existingNodes = _nodeRepository.GetInstances()
                    .Where(n => n.Area == area.ResRef.Value)
                    .ToList();

                if (existingNodes.Any())
                {
                    Log.Info($"Area {area.ResRef} already has {existingNodes.Count} nodes. Skipping provisioning.");
                    return CommandResult.Ok(new Dictionary<string, object>
                    {
                        ["provisionedCount"] = 0,
                        ["existingCount"] = existingNodes.Count,
                        ["skipped"] = true
                    });
                }
            }
            else
            {
                // Clear existing nodes if force respawn
                var existingNodes = _nodeRepository.GetInstances()
                    .Where(n => n.Area == area.ResRef.Value)
                    .ToList();

                foreach (var node in existingNodes)
                {
                    _nodeRepository.Delete(node);
                }

                Log.Info($"Cleared {existingNodes.Count} existing nodes from {area.ResRef} for force respawn");
            }

            Log.Info($"Provisioning nodes for area {area.ResRef}");

            // Get the NwArea to calculate bounds
            var nwArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == area.ResRef.Value);
            if (nwArea == null)
            {
                Log.Warn($"Area {area.ResRef} not loaded in module. Cannot provision nodes.");
                return CommandResult.Fail($"Area {area.ResRef} not found in module");
            }

            // Process each resource definition tag for this area
            foreach (var definitionTag in area.DefinitionTags)
            {
                var nodeDefinition = _definitionRepository.Get(definitionTag);
                if (nodeDefinition == null)
                {
                    Log.Warn($"Node definition not found for tag: {definitionTag}");
                    continue;
                }

                // Determine how many nodes to spawn (could be enhanced with config)
                int nodesToSpawn = CalculateNodeCount(nodeDefinition, area, nwArea);

                Log.Info($"Spawning {nodesToSpawn} {nodeDefinition.Name} nodes in {area.ResRef}");

                for (int i = 0; i < nodesToSpawn; i++)
                {
                    var position = GenerateRandomPosition(nwArea);
                    var rotation = (float)(_random.NextDouble() * 360);

                    try
                    {
                        // Create and spawn the node using the existing service
                        var node = _nodeService.CreateNewNode(area, nodeDefinition, position, rotation);
                        _nodeService.SpawnInstance(node);
                        provisionedNodes.Add(node);

                        Log.Debug($"Spawned {nodeDefinition.Name} at ({position.X:F1}, {position.Y:F1}) in {area.ResRef}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to spawn node {nodeDefinition.Name} in {area.ResRef}");
                    }
                }
            }

            // Publish event about successful provisioning
            await _eventBus.PublishAsync(new AreaNodesProvisionedEvent(
                area.ResRef.Value,
                nwArea.Name,
                provisionedNodes.Count,
                DateTime.UtcNow
            ));

            Log.Info($"Successfully provisioned {provisionedNodes.Count} nodes in {area.ResRef}");

            return CommandResult.Ok(new Dictionary<string, object>
            {
                ["provisionedCount"] = provisionedNodes.Count,
                ["nodeIds"] = provisionedNodes.Select(n => n.Id).ToList(),
                ["skipped"] = false
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to provision nodes for area {command.AreaDefinition.ResRef}");
            return CommandResult.Fail($"Failed to provision nodes: {ex.Message}");
        }
    }

    private int CalculateNodeCount(ResourceNodeDefinition definition, AreaDefinition area, NwArea nwArea)
    {
        // Base count on area size - larger areas get more nodes
        int areaSize = nwArea.Size.X * nwArea.Size.Y;

        // Calculate base nodes: ~1 node per 100 tiles (adjustable)
        int baseCount = Math.Max(1, areaSize / 100);

        // Add some randomization (±25%)
        int variance = Math.Max(1, baseCount / 4);
        int count = _random.Next(baseCount - variance, baseCount + variance + 1);

        // Cap at reasonable limits
        return Math.Clamp(count, 1, 20);
    }

    private Vector3 GenerateRandomPosition(NwArea nwArea)
    {
        // Generate random position within area bounds
        // Note: This is simplified - could be enhanced with:
        // - Walkable surface detection
        // - Exclusion zones (buildings, water, etc.)
        // - Clustering for more realistic distribution
        // - Minimum distance between nodes

        float x = (float)(_random.NextDouble() * nwArea.Size.X * 10f); // NWN uses 10 units per tile
        float y = (float)(_random.NextDouble() * nwArea.Size.Y * 10f);
        float z = 0f; // Ground level - could use GetSurfaceHeight for terrain following

        return new Vector3(x, y, z);
    }
}

