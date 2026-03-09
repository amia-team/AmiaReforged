using System.Numerics;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Handlers;

/// <summary>
/// Interaction handler for Prospecting — a multi-round interaction where a character
/// surveys a trigger zone and discovers new resource nodes.
/// <para>
/// <b>Preconditions:</b> Character must be a member of an industry that grants prospecting.
/// </para>
/// <para>
/// <b>Metadata keys expected in <see cref="InteractionContext.Metadata"/>:</b>
/// <list type="bullet">
///   <item><c>"allowedTypes"</c> — comma-separated <see cref="ResourceType"/> names the trigger allows
///         (e.g., <c>"Ore,Geode"</c>). If missing, all types from the area's definitions are used.</item>
///   <item><c>"spawnX"</c>, <c>"spawnY"</c>, <c>"spawnZ"</c> — position within the trigger to place 
///         discovered nodes. Defaults to (0, 0, 0) if missing.</item>
/// </list>
/// </para>
/// </summary>
[ServiceBinding(typeof(IInteractionHandler))]
public sealed class ProspectInteractionHandler(
    IRegionRepository regionRepository,
    IResourceNodeDefinitionRepository definitionRepository,
    ResourceNodeService nodeService,
    IEventBus eventBus) : IInteractionHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Base node count range before proficiency bonus.</summary>
    private const int BaseNodeCountMin = 1;
    private const int BaseNodeCountMax = 3;

    /// <summary>Default number of rounds to prospect, before proficiency adjustments.</summary>
    private const int BaseRounds = 6;

    public string InteractionTag => "prospecting";
    public InteractionTargetMode TargetMode => InteractionTargetMode.Trigger;

    public PreconditionResult CanStart(ICharacter character, InteractionContext context)
    {
        if (string.IsNullOrEmpty(context.AreaResRef))
        {
            return PreconditionResult.Fail("Area information is required for prospecting");
        }

        // Must belong to at least one industry (i.e., not a complete novice)
        List<IndustryMembership> memberships = character.AllIndustryMemberships();
        if (memberships.Count == 0)
        {
            return PreconditionResult.Fail("You must be a member of an industry to prospect");
        }

        // Check that the area is part of a known region
        if (!regionRepository.TryGetRegionForArea(context.AreaResRef, out _))
        {
            return PreconditionResult.Fail("This area is not part of a known region");
        }

        return PreconditionResult.Success();
    }

    public int CalculateRequiredRounds(ICharacter character, InteractionContext context)
    {
        ProficiencyLevel bestLevel = GetBestProficiency(character);

        // Higher proficiency → fewer rounds (minimum 2)
        int roundReduction = (int)bestLevel;
        return Math.Max(2, BaseRounds - roundReduction);
    }

    public TickResult OnTick(InteractionSession session, ICharacter character)
    {
        int newProgress = session.IncrementProgress(1);
        InteractionStatus status = session.IsComplete
            ? InteractionStatus.Completed
            : InteractionStatus.Active;

        string? message = status == InteractionStatus.Active
            ? $"Prospecting... ({newProgress}/{session.RequiredRounds})"
            : null;

        return new TickResult(status, newProgress, session.RequiredRounds, message);
    }

    public async Task<InteractionOutcome> OnCompleteAsync(
        InteractionSession session,
        ICharacter character,
        CancellationToken ct = default)
    {
        string areaResRef = GetAreaResRef(session);

        // Find area definition
        AreaDefinition? area = FindAreaDefinition(areaResRef);
        if (area is null)
        {
            return InteractionOutcome.Failed("Area definition not found");
        }

        // Determine which resource types are allowed
        HashSet<ResourceType> allowedTypes = GetAllowedTypes(session);

        // Find candidate node definitions
        List<ResourceNodeDefinition> candidates = GetCandidateDefinitions(area, allowedTypes);
        if (candidates.Count == 0)
        {
            return InteractionOutcome.Failed("No resources found in this area");
        }

        // Roll node count (base + proficiency bonus)
        ProficiencyLevel proficiency = GetBestProficiency(character);
        int proficiencyBonus = Math.Max(0, (int)proficiency - 1); // Apprentice+ gets bonus nodes
        int nodeCount = Random.Shared.Next(BaseNodeCountMin, BaseNodeCountMax + 1) + proficiencyBonus;
        nodeCount = Math.Min(nodeCount, candidates.Count); // Can't exceed available definitions

        // Spawn nodes
        Vector3 spawnPos = GetSpawnPosition(session);
        List<ProspectedNodeInfo> spawnedNodes = [];

        for (int i = 0; i < nodeCount; i++)
        {
            ResourceNodeDefinition definition = candidates[Random.Shared.Next(candidates.Count)];

            // Offset each node slightly to avoid stacking
            Vector3 offset = new(
                (float)(Random.Shared.NextDouble() * 4 - 2),
                (float)(Random.Shared.NextDouble() * 4 - 2),
                0);

            try
            {
                ResourceNodeInstance node = nodeService.CreateNewNode(
                    area, definition, spawnPos + offset);
                nodeService.SpawnInstance(node);

                spawnedNodes.Add(new ProspectedNodeInfo(
                    node.Id, definition.Tag, definition.Name));

                Log.Info("Prospecting spawned node '{Tag}' ({Id}) in area {Area}",
                    definition.Tag, node.Id, areaResRef);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to spawn prospected node '{Tag}' in area {Area}",
                    definition.Tag, areaResRef);
            }
        }

        if (spawnedNodes.Count == 0)
        {
            return InteractionOutcome.Failed("Failed to discover any resources");
        }

        await eventBus.PublishAsync(new ProspectingCompletedEvent(
            session.CharacterId,
            areaResRef,
            spawnedNodes.ToArray(),
            DateTime.UtcNow), ct);

        return InteractionOutcome.Succeeded(
            $"Discovered {spawnedNodes.Count} resource node(s)",
            new Dictionary<string, object>
            {
                ["nodesSpawned"] = spawnedNodes
            });
    }

    public void OnCancel(InteractionSession session, ICharacter character)
    {
        Log.Debug("Prospecting cancelled for character {CharacterId}", session.CharacterId);
    }

    // === Private helpers ===

    private static ProficiencyLevel GetBestProficiency(ICharacter character)
    {
        List<IndustryMembership> memberships = character.AllIndustryMemberships();
        if (memberships.Count == 0) return ProficiencyLevel.Layman;
        return memberships.Max(m => m.Level);
    }

    private AreaDefinition? FindAreaDefinition(string areaResRef)
    {
        foreach (RegionDefinition region in regionRepository.All())
        {
            AreaDefinition? area = region.Areas.FirstOrDefault(a => a.ResRef == areaResRef);
            if (area is not null) return area;
        }

        return null;
    }

    private List<ResourceNodeDefinition> GetCandidateDefinitions(
        AreaDefinition area, HashSet<ResourceType> allowedTypes)
    {
        List<ResourceNodeDefinition> candidates = [];

        foreach (string tag in area.DefinitionTags)
        {
            ResourceNodeDefinition? def = definitionRepository.Get(tag);
            if (def is null) continue;

            if (allowedTypes.Count == 0 || allowedTypes.Contains(def.Type))
            {
                candidates.Add(def);
            }
        }

        return candidates;
    }

    private static HashSet<ResourceType> GetAllowedTypes(InteractionSession session)
    {
        if (session.Metadata?.TryGetValue("allowedTypes", out object? value) == true
            && value is string typesStr && !string.IsNullOrEmpty(typesStr))
        {
            HashSet<ResourceType> types = [];
            foreach (string part in typesStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<ResourceType>(part, ignoreCase: true, out ResourceType parsed))
                {
                    types.Add(parsed);
                }
            }
            return types;
        }

        return [];
    }

    private static string GetAreaResRef(InteractionSession session)
    {
        return session.AreaResRef ?? string.Empty;
    }

    private static Vector3 GetSpawnPosition(InteractionSession session)
    {
        if (session.Metadata is null) return Vector3.Zero;

        float x = session.Metadata.TryGetValue("spawnX", out object? sx) && sx is float fx ? fx : 0f;
        float y = session.Metadata.TryGetValue("spawnY", out object? sy) && sy is float fy ? fy : 0f;
        float z = session.Metadata.TryGetValue("spawnZ", out object? sz) && sz is float fz ? fz : 0f;

        return new Vector3(x, y, z);
    }
}
