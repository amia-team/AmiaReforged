using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Stub implementation of the Harvesting subsystem.
/// TODO: Wire up to existing harvesting command and query handlers.
/// </summary>
[ServiceBinding(typeof(IHarvestingSubsystem))]
public sealed class HarvestingSubsystem : IHarvestingSubsystem
{
    public Task<CommandResult> SpawnResourceNodeAsync(string nodeType, string areaTag, float x, float y, float z, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> DespawnResourceNodeAsync(string nodeId, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<SpawnedNode?> GetResourceNodeAsync(string nodeId, CancellationToken ct = default)
    {
        return Task.FromResult<SpawnedNode?>(null);
    }

    public Task<List<SpawnedNode>> GetAreaResourceNodesAsync(string areaTag, CancellationToken ct = default)
    {
        return Task.FromResult(new List<SpawnedNode>());
    }

    public Task<HarvestResult> HarvestResourceAsync(CharacterId characterId, string nodeId, CancellationToken ct = default)
    {
        return Task.FromResult(HarvestResult.NoTool);
    }

    public Task<bool> CanHarvestAsync(CharacterId characterId, string nodeId, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<HarvestContext?> GetHarvestContextAsync(CharacterId characterId, string nodeId, CancellationToken ct = default)
    {
        return Task.FromResult<HarvestContext?>(null);
    }

    public Task<List<HarvestHistoryEntry>> GetHarvestHistoryAsync(CharacterId characterId, int limit = 50, CancellationToken ct = default)
    {
        return Task.FromResult(new List<HarvestHistoryEntry>());
    }

    public Task<DateTime?> GetLastHarvestTimeAsync(CharacterId characterId, string nodeId, CancellationToken ct = default)
    {
        return Task.FromResult<DateTime?>(null);
    }
}

