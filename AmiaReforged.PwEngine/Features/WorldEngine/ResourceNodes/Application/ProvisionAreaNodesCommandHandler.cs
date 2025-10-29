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
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Application;

[ServiceBinding(typeof(ICommandHandler<ProvisionAreaNodesCommand>))]
public class ProvisionAreaNodesCommandHandler : ICommandHandler<ProvisionAreaNodesCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly ResourceNodeService _nodeService;
    private readonly IResourceNodeInstanceRepository _nodeRepository;
    private readonly IResourceNodeDefinitionRepository _definitionRepository;
    private readonly TriggerBasedSpawnService _triggerSpawnService;
    private readonly IEventBus _eventBus;

    public ProvisionAreaNodesCommandHandler(
        ResourceNodeService nodeService,
        IResourceNodeInstanceRepository nodeRepository,
        IResourceNodeDefinitionRepository definitionRepository,
        TriggerBasedSpawnService triggerSpawnService,
        IEventBus eventBus)
    {
        _nodeService = nodeService;
        _nodeRepository = nodeRepository;
        _definitionRepository = definitionRepository;
        _triggerSpawnService = triggerSpawnService;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(ProvisionAreaNodesCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            AreaDefinition area = command.AreaDefinition;
            List<ResourceNodeInstance> provisionedNodes = new List<ResourceNodeInstance>();

            // Get the NwArea
            NwArea? nwArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == area.ResRef.Value);
            if (nwArea == null)
            {
                Log.Warn($"Area {area.ResRef} not loaded in module. Cannot provision nodes.");
                return CommandResult.Fail($"Area {area.ResRef} not found in module");
            }

            // Check if area already has nodes (unless forcing respawn)
            if (!command.ForceRespawn)
            {
                List<ResourceNodeInstance> existingNodes = _nodeRepository.GetInstances()
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
                List<ResourceNodeInstance> existingNodes = _nodeRepository.GetInstances()
                    .Where(n => n.Area == area.ResRef.Value)
                    .ToList();

                foreach (ResourceNodeInstance node in existingNodes)
                {
                    _nodeRepository.Delete(node);
                }

                Log.Info($"Cleared {existingNodes.Count} existing nodes from {area.ResRef} for force respawn");
            }

            Log.Info($"=== Provisioning nodes for area {area.ResRef} ({nwArea.Name}) ===");

            // Find all resource zone triggers in this area
            List<NwTrigger> triggers = _triggerSpawnService.GetResourceTriggers(nwArea);

            if (!triggers.Any())
            {
                Log.Warn($"No resource zone triggers found in area {area.ResRef}. " +
                         $"Triggers must be tagged '{WorldConstants.ResourceNodeZoneTag}'");
                return CommandResult.Ok(new Dictionary<string, object>
                {
                    ["provisionedCount"] = 0,
                    ["skipped"] = true,
                    ["reason"] = "no_triggers"
                });
            }

            Log.Info($"Found {triggers.Count} resource zone trigger(s) in {area.ResRef}");

            // Process each trigger
            foreach (NwTrigger trigger in triggers)
            {
                // Get TYPE filters from trigger (e.g., "ore,tree,geode")
                string nodeTypesStr = NWScript.GetLocalString(trigger, WorldConstants.LvarNodeTags);

                if (string.IsNullOrWhiteSpace(nodeTypesStr))
                {
                    Log.Warn($"Trigger {trigger.Tag} has no '{WorldConstants.LvarNodeTags}' local variable set. Skipping.");
                    continue;
                }

                // Remove quotes if present (defensive)
                nodeTypesStr = nodeTypesStr.Trim().Trim('"', '\'');

                List<string> typeFilters = nodeTypesStr.Split(',')
                    .Select(t => t.Trim().ToLower())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                if (!typeFilters.Any())
                {
                    Log.Warn($"Trigger {trigger.Tag} has empty node_tags. Skipping.");
                    continue;
                }

                // Get the actual node definition tags from the area JSON
                if (!area.DefinitionTags.Any())
                {
                    Log.Warn($"Area {area.ResRef} has no DefinitionTags defined");
                    continue;
                }

                // Filter area's DefinitionTags by matching their Type to trigger's type filters
                List<string> matchingDefinitionTags = new List<string>();
                foreach (string definitionTag in area.DefinitionTags)
                {
                    ResourceNodeDefinition? definition = _definitionRepository.Get(definitionTag);
                    if (definition == null)
                    {
                        Log.Warn($"  ✗ {definitionTag}: Definition not found in repository");
                        continue;
                    }

                    // Compare the definition's Type field to the trigger's type filters
                    string defType = definition.Type.ToString().ToLower();
                    if (typeFilters.Contains(defType))
                    {
                        matchingDefinitionTags.Add(definitionTag);
                        Log.Debug($"  ✓ {definitionTag}: Type '{definition.Type}' matches filter");
                    }
                }

                if (!matchingDefinitionTags.Any())
                {
                    Log.Info($"Trigger {trigger.Tag} type filters [{string.Join(", ", typeFilters)}] don't match any area definition Types");
                    continue;
                }

                // Get max nodes (default 5) using NWScript
                int maxNodes = NWScript.GetLocalInt(trigger, WorldConstants.LvarMaxNodesTotal);
                if (maxNodes <= 0)
                    maxNodes = WorldConstants.DefaultMaxNodesPerTrigger;

                Log.Info($"Processing trigger '{trigger.Tag}': type filters=[{string.Join(", ", typeFilters)}], matched {matchingDefinitionTags.Count} definition(s), max={maxNodes}");

                // Generate spawn locations using the SPECIFIC definition tags (not generic types)
                List<SpawnLocation> spawnLocations = _triggerSpawnService.GenerateSpawnLocations(
                    trigger,
                    matchingDefinitionTags,  // Pass specific tags like "ore_vein_copper_native", not "ore"
                    maxNodes
                );

                // Create and spawn nodes at each location
                foreach (SpawnLocation location in spawnLocations)
                {
                    // Now location.NodeTag should be a specific tag like "ore_vein_copper_native"
                    ResourceNodeDefinition? nodeDefinition = _definitionRepository.Get(location.NodeTag);

                    if (nodeDefinition == null)
                    {
                        Log.Warn($"Node definition not found for tag: {location.NodeTag}");
                        continue;
                    }

                    try
                    {
                        ResourceNodeInstance node = _nodeService.CreateNewNode(
                            area,
                            nodeDefinition,
                            location.Position,
                            location.Rotation
                        );

                        // TODO: Store trigger source metadata when ResourceNodeInstance supports it
                        // ...existing code...

                        _nodeService.SpawnInstance(node);
                        provisionedNodes.Add(node);

                        Log.Debug($"  ✓ Spawned {nodeDefinition.Name} (Type: {nodeDefinition.Type}) at ({location.Position.X:F1}, {location.Position.Y:F1})");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to spawn node {nodeDefinition.Name} at trigger {trigger.Tag}");
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

            Log.Info($"=== Successfully provisioned {provisionedNodes.Count} nodes in {area.ResRef} ===");

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
}

