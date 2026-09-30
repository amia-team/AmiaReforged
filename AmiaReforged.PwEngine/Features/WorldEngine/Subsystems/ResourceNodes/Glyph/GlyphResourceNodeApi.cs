using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Glyph;

[ServiceBinding(typeof(IGlyphResourceNodeApi))]
public sealed class GlyphResourceNodeApi : IGlyphResourceNodeApi
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IRegionRepository _regionRepository;
    private readonly IResourceNodeDefinitionRepository _definitionRepository;
    private readonly ResourceNodeService _nodeService;
    private readonly TriggerBasedSpawnService _triggerSpawnService;
    private readonly RuntimeNodeService _runtimeNodes;

    public GlyphResourceNodeApi(IRegionRepository regionRepository, IResourceNodeDefinitionRepository definitionRepository, ResourceNodeService nodeService, TriggerBasedSpawnService triggerSpawnService, RuntimeNodeService runtimeNodes)
    {
        _regionRepository = regionRepository;
        _definitionRepository = definitionRepository;
        _nodeService = nodeService;
        _triggerSpawnService = triggerSpawnService;
        _runtimeNodes = runtimeNodes;
    }

    /// <summary>Maximum number of live resource nodes of any single type allowed per area.</summary>
    private const int MaxNodesPerTypePerArea = 4;

    /// <inheritdoc />
    public SpawnResourceNodeOutcome SpawnResourceNode(uint triggerHandle)
    {
        try
        {
            // 1. Resolve trigger from the object handle
            NwTrigger? trigger = triggerHandle.ToNwObject<NwTrigger>();

            if (trigger == null)
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: handle {Handle} did not resolve to a valid trigger.",
                    triggerHandle);
                return new SpawnResourceNodeOutcome(false, "invalid_trigger", null);
            }

            // 2. Validate the trigger is a worldengine_node_region
            if (!string.Equals(trigger.Tag, "worldengine_node_region", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: trigger '{Tag}' is not tagged 'worldengine_node_region'.",
                    trigger.Tag);
                return new SpawnResourceNodeOutcome(false, "wrong_trigger_tag", null);
            }

            // 3. Read node_tags type filter from the trigger's local variable
            string nodeTypesStr = NWScript.GetLocalString(trigger, WorldConstants.LvarNodeTags);

            if (string.IsNullOrWhiteSpace(nodeTypesStr))
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: trigger has no '{Lvar}' local variable.",
                    WorldConstants.LvarNodeTags);
                return new SpawnResourceNodeOutcome(false, "missing_node_tags", null);
            }

            nodeTypesStr = nodeTypesStr.Trim().Trim('"', '\'');

            List<string> typeFilters = nodeTypesStr.Split(',')
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            if (typeFilters.Count == 0)
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: trigger has empty node_tags.");
                return new SpawnResourceNodeOutcome(false, "empty_node_tags", null);
            }

            // 4. Derive area ResRef from the trigger's location
            NwArea? nwArea = trigger.Area;
            if (nwArea == null)
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: trigger has no area.");
                return new SpawnResourceNodeOutcome(false, "no_area", null);
            }

            string areaResRef = nwArea.ResRef;

            AreaDefinition? area = FindAreaDefinition(areaResRef);
            if (area == null)
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: no AreaDefinition found for ResRef '{ResRef}'.",
                    areaResRef);
                return new SpawnResourceNodeOutcome(false, "no_area_definition", null);
            }

            if (area.DefinitionTags.Count == 0)
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: area '{ResRef}' has no DefinitionTags defined.",
                    areaResRef);
                return new SpawnResourceNodeOutcome(false, "no_definition_tags", null);
            }

            // 5. Filter area DefinitionTags by trigger's type filters (same logic as ProvisionAreaNodesCommandHandler)
            List<ResourceNodeDefinition> matchingDefinitions = [];
            foreach (string definitionTag in area.DefinitionTags)
            {
                ResourceNodeDefinition? definition = _definitionRepository.Get(definitionTag);
                if (definition == null) continue;

                string defType = definition.Type.ToString().ToLower();
                if (typeFilters.Contains(defType))
                {
                    matchingDefinitions.Add(definition);
                }
            }

            if (matchingDefinitions.Count == 0)
            {
                Log.Warn("[GlyphResourceNodeApi] SpawnResourceNode: no definitions in area '{ResRef}' match " +
                         "trigger type filters [{Filters}].",
                    areaResRef, string.Join(", ", typeFilters));
                return new SpawnResourceNodeOutcome(false, "no_matching_definitions", null);
            }

            // 6. Randomly select one definition
            ResourceNodeDefinition selected = matchingDefinitions[Random.Shared.Next(matchingDefinitions.Count)];

            // 6b. Enforce per-type cap: max 4 of each ResourceType alive in the area
            int currentCount = _runtimeNodes.CountNodesOfTypeInArea(areaResRef, selected.Type);
            if (currentCount >= MaxNodesPerTypePerArea)
            {
                Log.Info("[GlyphResourceNodeApi] SpawnResourceNode: cap reached for {Type} in area '{Area}' " +
                         "({Count}/{Max}).",
                    selected.Type, areaResRef, currentCount, MaxNodesPerTypePerArea);
                return new SpawnResourceNodeOutcome(false, $"cap_reached:{selected.Type}", null);
            }

            // 7. Generate a spawn position inside the trigger
            System.Numerics.Vector3 position = _triggerSpawnService.GetRandomPointInTrigger(trigger);
            float rotation = (float)(Random.Shared.NextDouble() * 360);

            // 8. Create the node instance (persists to DB) and spawn the in-game placeable
            ResourceNodeInstance? node = _nodeService.CreateNewNode(area, selected, position, rotation);
            if (node == null) return new SpawnResourceNodeOutcome(false, "failed_to_create_node", null);

            _nodeService.SpawnInstance(node);

            string qualityLabel = QualityLabel.QualityLabelForNode(selected.Type, node.Quality);

            Log.Info("[GlyphResourceNodeApi] SpawnResourceNode: spawned '{Name}' ({Tag}) at ({X:F1}, {Y:F1}, {Z:F1}) " +
                     "in area '{Area}', quality={Quality}.",
                selected.Name, selected.Tag, position.X, position.Y, position.Z, areaResRef, qualityLabel);

            SpawnResourceNodeResult result = new(
                node.Id,
                $"{qualityLabel} {selected.Name}",
                selected.Tag,
                qualityLabel,
                node.Uses,
                position.X,
                position.Y,
                position.Z);

            return new SpawnResourceNodeOutcome(true, null, result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphResourceNodeApi] SpawnResourceNode failed for trigger handle {Handle}.",
                triggerHandle);
            return new SpawnResourceNodeOutcome(false, "exception", null);
        }
    }

    /// <summary>
    /// Finds an <see cref="AreaDefinition"/> by its area ResRef across all registered regions.
    /// </summary>
    private AreaDefinition? FindAreaDefinition(string areaResRef)
    {
        if (_regionRepository.TryGetRegionForArea(areaResRef, out RegionDefinition? region) && region != null)
        {
            return region.Areas.FirstOrDefault(a =>
                string.Equals(a.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    /// <inheritdoc />
    public string? GetResourceNodeType(uint objectHandle)
    {
        try
        {
            NwObject? obj = objectHandle.ToNwObject<NwObject>();
            if (obj == null) return null;

            string tag = obj.Tag;
            if (string.IsNullOrWhiteSpace(tag)) return null;

            ResourceNodeDefinition? definition = _definitionRepository.Get(tag);
            if (definition == null) return null;
            if (definition.Type == ResourceType.Undefined) return null;

            return definition.Type.ToString();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[GlyphResourceNodeApi] GetResourceNodeType failed for handle {Handle}.", objectHandle);
            return null;
        }
    }
}
