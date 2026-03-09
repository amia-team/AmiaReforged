using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Tracks the in-flight state of a single character's active interaction.
/// Mutable by design — <see cref="IInteractionHandler.OnTick"/> updates <see cref="Progress"/>.
/// </summary>
public sealed class InteractionSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required CharacterId CharacterId { get; init; }
    public required string InteractionTag { get; init; }
    public required Guid TargetId { get; init; }
    public required InteractionTargetMode TargetMode { get; init; }

    /// <summary>Area ResRef where this interaction is taking place (if applicable).</summary>
    public string? AreaResRef { get; init; }

    /// <summary>Handler-specific data carried from the original command context.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>How many ticks / rounds of work have been completed so far.</summary>
    public int Progress { get; set; }

    /// <summary>Total ticks / rounds needed to finish (set once at session start).</summary>
    public required int RequiredRounds { get; init; }

    /// <summary>UTC timestamp of session creation.</summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Current lifecycle state of this session.</summary>
    public InteractionStatus Status { get; set; } = InteractionStatus.Active;

    /// <summary>
    /// Increments <see cref="Progress"/> by <paramref name="amount"/> and returns the new value.
    /// Does not change <see cref="Status"/>.
    /// </summary>
    public int IncrementProgress(int amount = 1)
    {
        Progress += amount;
        return Progress;
    }

    /// <summary>Returns <c>true</c> when <see cref="Progress"/> ≥ <see cref="RequiredRounds"/>.</summary>
    public bool IsComplete => Progress >= RequiredRounds;
}
