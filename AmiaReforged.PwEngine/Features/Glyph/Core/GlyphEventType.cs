namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// The encounter lifecycle events that a Glyph graph can attach to.
/// Determines when the graph's entry-point node fires.
/// </summary>
public enum GlyphEventType
{
    /// <summary>
    /// Fires before a spawn group's creatures are created.
    /// Can modify spawn count or cancel the spawn entirely.
    /// </summary>
    BeforeGroupSpawn,

    /// <summary>
    /// Fires after a spawn group's creatures have been created and placed in the world.
    /// Can modify spawned creatures, apply effects, or trigger world interactions.
    /// </summary>
    AfterGroupSpawn,

    /// <summary>
    /// Fires when a creature spawned by the dynamic encounter system dies.
    /// Provides the dead creature, killer, and encounter context.
    /// </summary>
    OnCreatureDeath,

    /// <summary>
    /// Fires when a trait is granted to a character.
    /// Provides the character ID, trait tag, and target creature reference.
    /// </summary>
    OnTraitGranted,

    /// <summary>
    /// Fires when a trait is removed from a character.
    /// Provides the character ID, trait tag, and target creature reference.
    /// </summary>
    OnTraitRemoved,

    /// <summary>
    /// Fires when a character attempts to start an interaction, before precondition checks.
    /// The script can block the interaction from starting by setting ShouldBlockInteraction.
    /// </summary>
    OnInteractionAttempted,

    /// <summary>
    /// Fires after an interaction session has been created and the first tick is about to run.
    /// Can set up visual effects, messages, or modify context variables.
    /// </summary>
    OnInteractionStarted,

    /// <summary>
    /// Fires each round/tick of an active interaction.
    /// Can perform per-tick logic or conditionally cancel the interaction mid-progress.
    /// </summary>
    OnInteractionTick,

    /// <summary>
    /// Fires when an interaction session completes (all rounds finished).
    /// Runs before the data-driven response system to augment the outcome.
    /// </summary>
    OnInteractionCompleted
}
