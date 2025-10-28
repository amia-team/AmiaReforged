using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;

/// <summary>
/// Command to harvest resources from a node
/// </summary>
public sealed record HarvestResourceCommand(
    Guid HarvesterId,
    Guid NodeInstanceId) : ICommand;

