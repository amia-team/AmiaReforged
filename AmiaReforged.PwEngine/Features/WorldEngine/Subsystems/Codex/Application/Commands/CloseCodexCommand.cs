using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;

/// <summary>
/// Command to close the player's Codex window (if open).
/// </summary>
public record CloseCodexCommand : ICommand
{
    /// <summary>The player whose Codex window should be closed.</summary>
    public required NwPlayer Player { get; init; }
}
