using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Commands;

/// <summary>
/// Command to register a new region definition in the world.
/// </summary>
public sealed record RegisterRegionCommand(
    RegionTag Tag,
    string Name,
    List<AreaDefinition> Areas) : ICommand;
