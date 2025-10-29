using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Commands;

/// <summary>
/// Command to update an existing region's definition.
/// </summary>
public sealed record UpdateRegionCommand(
    RegionTag Tag,
    string? Name = null,
    List<AreaDefinition>? Areas = null,
    List<SettlementId>? Settlements = null) : ICommand;

