using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;
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
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;

    public RegionSubsystem(
        IRegionRepository regionRepository,
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher)
    {
        _regionRepository = regionRepository;
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
    }

    /// <inheritdoc/>
    public async Task<RegionInfo?> GetRegionAsync(string regionTag, CancellationToken ct = default)
    {
        // Routes through the existing GetRegionDefinitionQuery (case-insensitive by tag).
        RegionDefinition? definition = await _queryDispatcher
            .DispatchAsync<GetRegionDefinitionQuery, RegionDefinition?>(
                new GetRegionDefinitionQuery { Tag = regionTag }, ct);

        return definition is null ? null : ToRegionInfo(definition);
    }

    /// <inheritdoc/>
    public async Task<List<RegionInfo>> GetAllRegionsAsync(CancellationToken ct = default)
    {
        // Routes through the existing SearchRegionDefinitionsQuery with an empty term (returns all).
        List<RegionDefinition> definitions = await _queryDispatcher
            .DispatchAsync<SearchRegionDefinitionsQuery, List<RegionDefinition>>(
                new SearchRegionDefinitionsQuery { SearchTerm = null }, ct);

        return definitions.Select(ToRegionInfo).ToList();
    }

    /// <summary>
    /// Maps a <see cref="RegionDefinition"/> to the region facade projection.
    /// Every facade field has defined storage or an explicit compatibility/default rule
    /// (region contract task 033).
    /// </summary>
    private static RegionInfo ToRegionInfo(RegionDefinition definition)
    {
        return new RegionInfo(
            definition.Tag.Value,
            definition.Name,
            definition.Description ?? string.Empty,
            definition.Type ?? RegionType.Special);
    }

    public async Task<CommandResult> UpdateRegionAsync(UpdateRegionCommand command, CancellationToken ct = default)
    {
        // Translate the reconciled facade request (task 033) into the application persistence
        // command. The current definition is loaded only to merge partial changes onto it; the
        // application handler owns existence validation and persistence (task 035).
        if (string.IsNullOrWhiteSpace(command.RegionTag))
        {
            return CommandResult.Fail("Region tag cannot be empty");
        }

        RegionDefinition? current = await _queryDispatcher
            .DispatchAsync<GetRegionDefinitionQuery, RegionDefinition?>(
                new GetRegionDefinitionQuery { Tag = command.RegionTag }, ct);

        RegionDefinition updated = new()
        {
            Tag = current?.Tag ?? new RegionTag(command.RegionTag),
            Name = command.Name ?? current?.Name ?? string.Empty,
            Description = command.Description ?? current?.Description,
            Type = command.Type ?? current?.Type,
            Areas = current?.Areas ?? new List<AreaDefinition>(),
            DefaultChaos = current?.DefaultChaos
        };

        Application.Regions.Commands.UpdateRegionCommand appCommand = new()
        {
            Tag = command.RegionTag,
            Definition = updated
        };

        // Dispatch; the handler fails (without insertion) when the tag is unknown and publishes
        // the generic CommandExecutedEvent on success.
        return await _commandDispatcher.DispatchAsync(appCommand, ct);
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
        // Resolution (area override -> region default -> ChaosState.Default) lives in the query
        // handler; the subsystem only dispatches (task 036).
        return _queryDispatcher.DispatchAsync<GetChaosForAreaQuery, ChaosState>(
            new GetChaosForAreaQuery(areaResRef), ct);
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
