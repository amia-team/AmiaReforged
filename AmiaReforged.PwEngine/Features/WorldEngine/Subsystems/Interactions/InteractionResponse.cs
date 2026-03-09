using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// One possible outcome of a data-driven interaction. When an interaction completes,
/// the <see cref="DataDrivenInteractionAdapter"/> selects one response via weighted random
/// from the eligible pool (filtered by <see cref="MinProficiency"/>).
/// </summary>
public class InteractionResponse
{
    /// <summary>
    /// Short identifier for this response within the parent <see cref="InteractionDefinition"/>.
    /// Used for logging and event tracing.
    /// </summary>
    public required string ResponseTag { get; init; }

    /// <summary>
    /// Relative weight for random selection. Higher weight → more likely.
    /// Defaults to 1 (equal chance with other weight-1 responses).
    /// </summary>
    public int Weight { get; init; } = 1;

    /// <summary>
    /// Minimum proficiency level required for this response to be eligible.
    /// <c>null</c> means no minimum — available to everyone.
    /// </summary>
    public ProficiencyLevel? MinProficiency { get; init; }

    /// <summary>
    /// Player-facing summary message shown when this response fires.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Effects that execute when this response is selected (VFX, text, spawns, etc.).
    /// </summary>
    public List<InteractionResponseEffect> Effects { get; init; } = [];
}
