namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Registry of all available <see cref="GlyphNodeDefinition"/>s in the system.
/// Node definitions are registered at startup and used by both the runtime interpreter
/// (to look up pin definitions) and the editor API (to populate the node palette).
/// </summary>
public interface IGlyphNodeDefinitionRegistry
{
    /// <summary>
    /// Registers a node definition. Throws if a definition with the same TypeId already exists.
    /// </summary>
    /// <param name="definition">The node definition to register</param>
    void Register(GlyphNodeDefinition definition);

    /// <summary>
    /// Gets a node definition by its type ID. Returns null if not found.
    /// </summary>
    /// <param name="typeId">The unique type identifier (e.g., "flow.branch")</param>
    GlyphNodeDefinition? Get(string typeId);

    /// <summary>
    /// Gets all registered node definitions.
    /// </summary>
    IReadOnlyList<GlyphNodeDefinition> GetAll();

    /// <summary>
    /// Gets all node definitions valid for a specific event type.
    /// Includes definitions with no event restriction and those restricted to the given type.
    /// </summary>
    /// <param name="eventType">The event type to filter by</param>
    IReadOnlyList<GlyphNodeDefinition> GetForEventType(GlyphEventType eventType);

    /// <summary>
    /// Gets all registered categories (for editor palette grouping).
    /// </summary>
    IReadOnlyList<string> GetCategories();
}
