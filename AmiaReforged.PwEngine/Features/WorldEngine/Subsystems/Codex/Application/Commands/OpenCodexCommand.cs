using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;

/// <summary>
/// Command to open the player's Codex window.
/// Enforces one-window-per-player: if a Codex is already open, returns a failure result.
/// </summary>
public record OpenCodexCommand : ICommand
{
    /// <summary>The player requesting the Codex window.</summary>
    public required NwPlayer Player { get; init; }
}
