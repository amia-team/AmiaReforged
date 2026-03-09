using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Manages the lifecycle of <see cref="InteractionSession"/> instances.
/// Enforces exclusive sessions — a character can have at most one active interaction.
/// </summary>
public interface IInteractionSessionManager
{
    /// <summary>
    /// Gets the active session for <paramref name="characterId"/>, or <c>null</c> if none exists.
    /// </summary>
    InteractionSession? GetActiveSession(CharacterId characterId);

    /// <summary>
    /// Returns <c>true</c> when the character currently has an active interaction session.
    /// </summary>
    bool HasActiveSession(CharacterId characterId);

    /// <summary>
    /// Creates and stores a new session. If the character already has an active session,
    /// it is silently replaced.
    /// </summary>
    InteractionSession StartSession(
        CharacterId characterId,
        string interactionTag,
        Guid targetId,
        InteractionTargetMode targetMode,
        int requiredRounds,
        string? areaResRef = null,
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Removes the active session for <paramref name="characterId"/>.
    /// No-op if no session exists.
    /// </summary>
    void EndSession(CharacterId characterId);
}
