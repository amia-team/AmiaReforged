using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Region subsystem wired to the in-memory region repository.
/// Provides area registration checks and chaos state resolution.
/// </summary>
[ServiceBinding(typeof(IRegionSubsystem))]
public sealed class RegionSubsystem : IRegionSubsystem
{
    private readonly IRegionRepository _regionRepository;

    public RegionSubsystem(IRegionRepository regionRepository)
    {
        _regionRepository = regionRepository;
    }
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
    /// Resolves the chaos state for an area. If the area is in a region, returns the area-level
    /// chaos override if present, otherwise the region's default chaos. If the area is NOT in
    /// any region, returns <see cref="ChaosState.Default"/> (all zeros).
    /// </summary>
    public Task<ChaosState> GetChaosForAreaAsync(string areaResRef, CancellationToken ct = default)
    {
        if (!_regionRepository.TryGetRegionForArea(areaResRef, out RegionDefinition? region) || region is null)
        {
            return Task.FromResult(ChaosState.Default);
        }

        // Look for an area-level chaos override
        AreaDefinition? areaDef = region.Areas
            .FirstOrDefault(a => string.Equals(a.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase));

        if (areaDef?.Environment.Chaos is { } areaChaos)
        {
            return Task.FromResult(areaChaos);
        }

        // Fall back to the region's default chaos, or ChaosState.Default
        return Task.FromResult(region.DefaultChaos ?? ChaosState.Default);
    }

    /// <inheritdoc/>
    public bool IsAreaInRegion(string areaResRef)
    {
        return _regionRepository.IsAreaRegistered(areaResRef);
    }

    /// <inheritdoc/>
    public string? GetRegionTagForArea(string areaResRef)
    {
        return _regionRepository.TryGetRegionForArea(areaResRef, out RegionDefinition? region) && region is not null
            ? region.Tag.Value
            : null;
    }
}
