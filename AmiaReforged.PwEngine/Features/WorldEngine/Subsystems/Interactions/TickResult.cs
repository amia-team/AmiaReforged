using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Returned by <see cref="IInteractionHandler.OnTick"/> to report what happened
/// during a single round of the interaction.
/// </summary>
public readonly record struct TickResult(
    InteractionStatus Status,
    int CurrentProgress,
    int RequiredRounds,
    string? Message = null);
