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
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>How many ticks / rounds of work have been completed so far.</summary>
    public int Progress { get; set; }

    /// <summary>Total ticks / rounds needed to finish. Settable by Glyph scripts.</summary>
    public required int RequiredRounds { get; set; }

    /// <summary>UTC timestamp of session creation.</summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Current lifecycle state of this session.</summary>
    public InteractionStatus Status { get; set; } = InteractionStatus.Active;

    /// <summary>
    /// Event types that have been suppressed by a Glyph script during this session.
    /// When an event type is in this set, the hook service will skip executing graphs for it.
    /// This enables cross-event pipeline control (e.g., OnStarted can suppress OnTick).
    /// </summary>
    public HashSet<string> SuppressedEventTypes { get; } = [];

    /// <summary>
    /// Adds an event type to the suppression set so it will not fire for this session.
    /// </summary>
    public void SuppressEvent(string eventType) => SuppressedEventTypes.Add(eventType);

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
