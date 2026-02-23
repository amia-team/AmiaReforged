using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Stub implementation of the Region subsystem.
/// TODO: Wire up to existing region management systems.
/// </summary>
[ServiceBinding(typeof(IRegionSubsystem))]
public sealed class RegionSubsystem : IRegionSubsystem
{
    public Task<RegionInfo?> GetRegionAsync(string regionTag, CancellationToken ct = default)
    {
        return Task.FromResult<RegionInfo?>(null);
    }

    public Task<List<RegionInfo>> GetAllRegionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new List<RegionInfo>());
    }

    public Task<CommandResult> UpdateRegionAsync(UpdateRegionCommand command, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> ApplyRegionalEffectAsync(string regionTag, string effectId, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> RemoveRegionalEffectAsync(string regionTag, string effectId, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<List<RegionalEffect>> GetRegionalEffectsAsync(string regionTag, CancellationToken ct = default)
    {
        return Task.FromResult(new List<RegionalEffect>());
    }

    /// <summary>
    /// Stub: returns default chaos state. TODO: Wire to InMemoryRegionRepository to resolve
    /// area-level overrides and region defaults.
    /// </summary>
    public Task<ChaosState> GetChaosForAreaAsync(string areaResRef, CancellationToken ct = default)
    {
        return Task.FromResult(ChaosState.Default);
    }
}

