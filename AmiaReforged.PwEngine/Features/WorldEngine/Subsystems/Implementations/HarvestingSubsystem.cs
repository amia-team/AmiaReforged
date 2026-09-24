using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Thin dispatch wrapper over the harvesting command/query handlers.
/// All supported operations route through the central dispatchers so
/// writes get logging, the exception-to-Fail contract, and
/// CommandExecutedEvent publishing.
///
/// Deliberately unsupported (no backing store or command exists):
/// <list type="bullet">
/// <item><see cref="SpawnResourceNodeAsync"/> — live spawn quality/uses are derived
/// from <c>AreaDefinition</c> via <c>ResourceNodeService</c>; this signature cannot
/// supply them, so no honest <c>RegisterNodeCommand</c> can be built here.</item>
/// <item>Harvest history / last-harvest time — no history repository or query exists.
/// These return empty/null rather than inventing persistence.</item>
/// </list>
/// Placeable-bound reads (<see cref="GetResourceNodeAsync"/>,
/// <see cref="GetAreaResourceNodesAsync"/>) wrap the queried
/// <c>ResourceNodeInstance</c> with a null placeable: runtime placeables only exist
/// in-game via <c>RuntimeNodeService</c>.
/// </summary>
[ServiceBinding(typeof(IHarvestingSubsystem))]
public sealed class HarvestingSubsystem : IHarvestingSubsystem
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public HarvestingSubsystem(
        ICommandDispatcher commands,
        IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    public Task<CommandResult> SpawnResourceNodeAsync(string nodeType, string areaTag, float x, float y, float z,
        CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail(
            "Not supported: node spawn quality/uses are derived from AreaDefinition via ResourceNodeService; use ProvisionAreaNodesCommand or RegisterNodeCommand directly."));
    }

    public Task<CommandResult> DespawnResourceNodeAsync(string nodeId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(nodeId, out Guid instanceId))
        {
            return Task.FromResult(CommandResult.Fail($"Invalid node id '{nodeId}'"));
        }

        return _commands.DispatchAsync(new DestroyNodeCommand(instanceId), ct);
    }

    public async Task<SpawnedNode?> GetResourceNodeAsync(string nodeId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(nodeId, out Guid instanceId))
        {
            return null;
        }

        ResourceNodeInstance? instance = await _queries
            .DispatchAsync<GetNodeByIdQuery, ResourceNodeInstance?>(new GetNodeByIdQuery(instanceId), ct);

        return instance is null ? null : new SpawnedNode(null, instance);
    }

    public async Task<List<SpawnedNode>> GetAreaResourceNodesAsync(string areaTag, CancellationToken ct = default)
    {
        List<ResourceNodeInstance> instances = await _queries
            .DispatchAsync<GetNodesForAreaQuery, List<ResourceNodeInstance>>(new GetNodesForAreaQuery(areaTag), ct);

        return instances.Select(i => new SpawnedNode(null, i)).ToList();
    }

    /// <summary>
    /// Drives one tick of the character's "harvesting" interaction session.
    /// Harvesting is an interaction: multi-round nodes need one call per round,
    /// mirroring how ingress adapters (e.g. one attack = one tick) drive it.
    /// </summary>
    public async Task<HarvestResult> HarvestResourceAsync(CharacterId characterId, string nodeId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(nodeId, out Guid instanceId))
        {
            return HarvestResult.Failed;
        }

        CommandResult result = await _commands
            .DispatchAsync(new PerformInteractionCommand(characterId, "harvesting", instanceId), ct);

        if (!result.Success)
        {
            return HarvestResult.Failed;
        }

        return (result.Data?.GetValueOrDefault("status") as string) switch
        {
            "InProgress" => HarvestResult.InProgress,
            _ => HarvestResult.Finished,
        };
    }

    public async Task<bool> CanHarvestAsync(CharacterId characterId, string nodeId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(nodeId, out Guid instanceId))
        {
            return false;
        }

        // Node-side check only: the node exists and has uses remaining.
        // Character/tool preconditions live in the interaction framework
        // (HarvestInteractionHandler.CanStart) and the command handler itself.
        NodeStateDto? state = await _queries
            .DispatchAsync<GetNodeStateQuery, NodeStateDto?>(new GetNodeStateQuery(instanceId), ct);

        return state is not null && state.RemainingUses > 0;
    }

    public async Task<HarvestContext?> GetHarvestContextAsync(CharacterId characterId, string nodeId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(nodeId, out Guid instanceId))
        {
            return null;
        }

        ResourceNodeInstance? instance = await _queries
            .DispatchAsync<GetNodeByIdQuery, ResourceNodeInstance?>(new GetNodeByIdQuery(instanceId), ct);

        return instance?.Definition.Requirement;
    }

    public Task<List<HarvestHistoryEntry>> GetHarvestHistoryAsync(CharacterId characterId, int limit = 50,
        CancellationToken ct = default)
    {
        return Task.FromResult(new List<HarvestHistoryEntry>());
    }

    public Task<DateTime?> GetLastHarvestTimeAsync(CharacterId characterId, string nodeId,
        CancellationToken ct = default)
    {
        return Task.FromResult<DateTime?>(null);
    }
}
