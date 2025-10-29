using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Services;

[ServiceBinding(typeof(ResourceNodeInstanceSetupService))]
public class ResourceNodeInstanceSetupService(
    IResourceNodeDefinitionRepository resourceRepository,
    ICommandHandler<ClearAreaNodesCommand> clearNodesCommandHandler,
    IQueryHandler<GetNodesForAreaQuery, List<ResourceNodeInstance>> getNodesQueryHandler,
    IRegionRepository regionRepository,
    ResourceNodeService nodeService,
    TriggerBasedSpawnService triggerSpawnService)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();


    public void DoSetup()
    {
        NwModule.Instance.SendMessageToAllDMs("INITIALIZING ECONOMY . . .");
        ClearOldNodes();
        foreach (RegionDefinition region in regionRepository.All())
        {
            foreach (AreaDefinition area in region.Areas)
            {
                GenerateNodes(area);
            }
        }
    }

    public void ClearOldNodes()
    {
        foreach (NwArea area in NwModule.Instance.Areas)
        {
            // Query for nodes in this area
            GetNodesForAreaQuery query = new GetNodesForAreaQuery(area.ResRef);
            List<ResourceNodeInstance> nodes = getNodesQueryHandler.HandleAsync(query).GetAwaiter().GetResult();

            if (nodes.Count == 0) continue;

            NwModule.Instance.SendMessageToAllDMs($"PURGING NODES IN {area.Name} . . .");

            // Execute clear command
            ClearAreaNodesCommand command = new ClearAreaNodesCommand(area.ResRef);
            _ = clearNodesCommandHandler.HandleAsync(command).GetAwaiter().GetResult();
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

        NwModule.Instance.SendMessageToAllDMs($"Processing {nwArea.Name} . . .");

        // Find all resource zone triggers in this area
        List<NwTrigger> nodeSpawnRegions = triggerSpawnService.GetResourceTriggers(nwArea);

        if (!nodeSpawnRegions.Any())
        {
            Log.Warn($"No resource zone triggers found in {nwArea.Name}");
            return;
        }

        NwModule.Instance.SendMessageToAllDMs($"Found {nodeSpawnRegions.Count} resource trigger(s) in {nwArea.Name}");

        GenerateNodesInTriggers(nodeSpawnRegions, area);
    }

    private void GenerateNodesInTriggers(List<NwTrigger> nodeSpawnRegions, AreaDefinition area)
    {
        foreach (NwTrigger trigger in nodeSpawnRegions)
        {
            ProcessTriggerNodes(trigger, area);
        }
    }

    private void ProcessTriggerNodes(NwTrigger trigger, AreaDefinition area)
    {
        // Get TYPE filters from trigger local variable (e.g., "ore,geode,tree")
        // Note: Variable is named "node_tags" but actually contains TYPE filters
        var nodeTypesStr = NWScript.GetLocalString(trigger, WorldConstants.LvarNodeTags);

        // Defensive parsing: handle null, empty, or whitespace
        if (string.IsNullOrWhiteSpace(nodeTypesStr))
        {
            Log.Warn($"Trigger {trigger.Tag} has no '{WorldConstants.LvarNodeTags}' local variable");
            return;
        }

        // Remove surrounding quotes if present (defensive against toolset variations)
        nodeTypesStr = nodeTypesStr.Trim().Trim('"', '\'');

        var allowedTypes = nodeTypesStr.Split(',')
            .Select(t => t.Trim().ToLower())
            .Where(t => !string.IsNullOrWhiteSpace(t))  // Skip empty entries
            .ToHashSet();

        if (!allowedTypes.Any())
        {
            Log.Warn($"Trigger {trigger.Tag} has empty node_tags after parsing");
            return;
        }

        // Get the actual node definition tags from the area JSON (e.g., "ore_vein_copper_native", "tree_oak")
        if (!area.DefinitionTags.Any())
        {
            Log.Warn($"Area {area.ResRef} has no DefinitionTags defined");
            return;
        }

        // Filter area's DefinitionTags by matching their Type to trigger's type filters
        var matchingDefinitionTags = new List<string>();
        Log.Info($"Checking {area.DefinitionTags.Count} definitions in area {area.ResRef} against type filters: [{string.Join(", ", allowedTypes)}]");

        foreach (var definitionTag in area.DefinitionTags)
        {
            Log.Info($" Checking definition tag: {definitionTag}");
            // Load the definition to check its Type field
            var definition = resourceRepository.Get(definitionTag);
            if (definition == null)
            {
                Log.Warn($"  ✗ {definitionTag}: Definition not found in repository");
                continue;
            }

            // Compare the definition's Type field to the trigger's type filters
            var defType = definition.Type.ToString().ToLower();
            if (allowedTypes.Contains(defType))
            {
                matchingDefinitionTags.Add(definitionTag);
                Log.Info($"  ✓ {definitionTag}: Type '{definition.Type}' matches filter");
            }
            else
            {
                Log.Debug($"  ✗ {definitionTag}: Type '{definition.Type}' does not match filters");
            }
        }

        if (!matchingDefinitionTags.Any())
        {
            Log.Info($"Trigger {trigger.Tag} type filters [{string.Join(", ", allowedTypes)}] don't match any area definition Types");
            return;
        }

        // Get max nodes (default 5)
        var maxNodes = NWScript.GetLocalInt(trigger, WorldConstants.LvarMaxNodesTotal);
        if (maxNodes <= 0)
            maxNodes = WorldConstants.DefaultMaxNodesPerTrigger;

        NwModule.Instance.SendMessageToAllDMs($"Trigger '{trigger.Tag}': type filters=[{string.Join(", ", allowedTypes)}], matched {matchingDefinitionTags.Count} definition(s), max={maxNodes}");

        // Generate spawn locations using the matching definition tags
        var spawnLocations = triggerSpawnService.GenerateSpawnLocations(
            trigger,
            matchingDefinitionTags,
            maxNodes
        );

        NwModule.Instance.SendMessageToAllDMs($"Generated {spawnLocations.Count} spawn location(s)");

        // Create and spawn nodes at each location
        int spawned = 0;
        foreach (var location in spawnLocations)
        {
            var nodeDefinition = resourceRepository.Get(location.NodeTag);

            if (nodeDefinition == null)
            {
                Log.Warn($"Node definition not found for tag: {location.NodeTag}");
                continue;
            }

            try
            {
                var node = nodeService.CreateNewNode(
                    area,
                    nodeDefinition,
                    location.Position,
                    location.Rotation
                );

                nodeService.SpawnInstance(node);
                spawned++;

                Log.Debug($"Spawned {nodeDefinition.Name} (Type: {nodeDefinition.Type}) at ({location.Position.X:F1}, {location.Position.Y:F1})");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to spawn node {nodeDefinition.Name}");
            }
        }

        NwModule.Instance.SendMessageToAllDMs($"✓ Spawned {spawned} node(s) in trigger '{trigger.Tag}'");
    }
}

