using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Commands;

/// <summary>
/// Command to remove a region from the world definition.
/// </summary>
public sealed record RemoveRegionCommand(RegionTag Tag) : ICommand;

