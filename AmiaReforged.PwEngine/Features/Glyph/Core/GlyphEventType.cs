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
    OnTraitRemoved
}
