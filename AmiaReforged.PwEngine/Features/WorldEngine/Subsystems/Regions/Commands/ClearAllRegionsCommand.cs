using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Commands;

/// <summary>
/// Command to clear all regions (typically used during reload operations).
/// </summary>
public sealed record ClearAllRegionsCommand : ICommand;

