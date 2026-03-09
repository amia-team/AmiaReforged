namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Categories of Glyph scripts that determine which nodes are available
/// in the editor palette and which context data is populated at runtime.
/// </summary>
public enum GlyphScriptCategory
{
    /// <summary>
    /// Scripts that modify or react to dynamic encounter spawns.
    /// </summary>
    Encounter,

    /// <summary>
    /// Scripts that fire when traits are granted/removed and can modify trait behavior.
    /// </summary>
    Trait,

    /// <summary>
    /// Scripts for area-level environmental effects (weather, lighting, ambient spawns).
    /// </summary>
    Environmental,

    /// <summary>
    /// Scripts for NPC dialogue, journal updates, and story-driven triggers.
    /// </summary>
    Narrative,

    /// <summary>
    /// Scripts that hook into the interaction framework lifecycle (attempt, start, tick, complete).
    /// </summary>
    Interaction
}

/// <summary>
/// Extension methods for mapping <see cref="GlyphEventType"/> to <see cref="GlyphScriptCategory"/>.
/// </summary>
public static class GlyphEventTypeExtensions
{
    /// <summary>
    /// Returns the script category that the given event type belongs to.
    /// </summary>
    public static GlyphScriptCategory GetCategory(this GlyphEventType eventType) => eventType switch
    {
        GlyphEventType.BeforeGroupSpawn => GlyphScriptCategory.Encounter,
        GlyphEventType.AfterGroupSpawn => GlyphScriptCategory.Encounter,
        GlyphEventType.OnCreatureDeath => GlyphScriptCategory.Encounter,
        GlyphEventType.OnTraitGranted => GlyphScriptCategory.Trait,
        GlyphEventType.OnTraitRemoved => GlyphScriptCategory.Trait,
        GlyphEventType.OnInteractionAttempted => GlyphScriptCategory.Interaction,
        GlyphEventType.OnInteractionStarted => GlyphScriptCategory.Interaction,
        GlyphEventType.OnInteractionTick => GlyphScriptCategory.Interaction,
        GlyphEventType.OnInteractionCompleted => GlyphScriptCategory.Interaction,
        _ => GlyphScriptCategory.Encounter
    };

    /// <summary>
    /// Returns all event types belonging to the given category.
    /// </summary>
    public static IReadOnlyList<GlyphEventType> GetEventTypes(this GlyphScriptCategory category) => category switch
    {
        GlyphScriptCategory.Encounter => [GlyphEventType.BeforeGroupSpawn, GlyphEventType.AfterGroupSpawn, GlyphEventType.OnCreatureDeath],
        GlyphScriptCategory.Trait => [GlyphEventType.OnTraitGranted, GlyphEventType.OnTraitRemoved],
        GlyphScriptCategory.Environmental => [],
        GlyphScriptCategory.Narrative => [],
        GlyphScriptCategory.Interaction => [GlyphEventType.OnInteractionAttempted, GlyphEventType.OnInteractionStarted, GlyphEventType.OnInteractionTick, GlyphEventType.OnInteractionCompleted],
        _ => []
    };
}
