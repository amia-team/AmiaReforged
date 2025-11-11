using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;

/// <summary>
/// Command to clear all resource nodes in an area
/// </summary>
public sealed record ClearAreaNodesCommand(string AreaResRef) : ICommand;

