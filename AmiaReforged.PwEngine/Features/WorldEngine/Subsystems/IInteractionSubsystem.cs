using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Subsystem providing access to the generic Interaction Framework.
/// Supports multi-round interactions like Harvesting and Prospecting,
/// with extensibility for future interaction types.
/// </summary>
public interface IInteractionSubsystem
{
    /// <summary>
    /// Performs one tick of the specified interaction. Creates a new session
    /// if the character doesn't have one for this tag+target, or advances
    /// an existing session. Cancels any active session for a different
    /// interaction type.
    /// </summary>
    Task<CommandResult> PerformInteractionAsync(
        CharacterId characterId,
        string interactionTag,
        Guid targetId,
        string? areaResRef = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether the character currently has an active interaction session.
    /// </summary>
    bool HasActiveInteraction(CharacterId characterId);

    /// <summary>
    /// Gets information about the character's current interaction, if any.
    /// </summary>
    InteractionInfo? GetActiveInteraction(CharacterId characterId);

    /// <summary>
    /// Returns the tags of all registered interaction types.
    /// </summary>
    IReadOnlyCollection<string> GetRegisteredInteractionTypes();
}

/// <summary>Summary of an active interaction session for querying.</summary>
public record InteractionInfo(
    string InteractionTag,
    Guid TargetId,
    int Progress,
    int RequiredRounds,
    DateTime StartedAt);
