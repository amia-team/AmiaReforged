using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Strategy interface for a specific interaction type (e.g., Harvesting, Prospecting).
/// Each implementation is a singleton registered via Anvil DI and looked up by
/// <see cref="InteractionTag"/> through <see cref="IInteractionHandlerRegistry"/>.
/// </summary>
public interface IInteractionHandler
{
    /// <summary>Unique tag identifying this interaction type (e.g., <c>"harvesting"</c>, <c>"prospecting"</c>).</summary>
    string InteractionTag { get; }

    /// <summary>What kind of game entity this interaction targets.</summary>
    InteractionTargetMode TargetMode { get; }

    /// <summary>
    /// Checks whether <paramref name="character"/> can begin this interaction
    /// with the given <paramref name="context"/>. Called before session creation.
    /// </summary>
    PreconditionResult CanStart(ICharacter character, InteractionContext context);

    /// <summary>
    /// Determines how many rounds this interaction requires for <paramref name="character"/>.
    /// Called once at session creation.
    /// </summary>
    int CalculateRequiredRounds(ICharacter character, InteractionContext context);

    /// <summary>
    /// Advances the interaction by one tick. The handler should update
    /// <see cref="InteractionSession.Progress"/> and return the resulting state.
    /// </summary>
    TickResult OnTick(InteractionSession session, ICharacter character);

    /// <summary>
    /// Called when progress reaches the required rounds. Performs the final
    /// domain action (e.g., give items, spawn nodes) and returns the outcome.
    /// </summary>
    Task<InteractionOutcome> OnCompleteAsync(InteractionSession session, ICharacter character,
        CancellationToken ct = default);

    /// <summary>
    /// Called when the session is cancelled before completion. Lets the handler
    /// perform cleanup (e.g., restore state, notify player).
    /// </summary>
    void OnCancel(InteractionSession session, ICharacter character);
}
