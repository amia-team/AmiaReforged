using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;

/// <summary>
/// Command to destroy a specific resource node
/// </summary>
public sealed record DestroyNodeCommand(Guid NodeInstanceId) : ICommand;

